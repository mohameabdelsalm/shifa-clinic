using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Clinical;
using Core.Entities.Clinical;
using Core.Enums.Clinical;
using Core.Enums.Scheduling;
using Core.Interfaces.Clinic;
using Core.Interfaces.Clinical;
using Core.Interfaces.Common;
using Core.Interfaces.Scheduling;
using Core.Models.Clinical;
using Infrastructure.Configurations.Clinical;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Clinical;

/// <summary>
/// Visits and their content (ADR-010, business rule 7).
/// <list type="bullet">
/// <item>Reads need the visit's branch in the caller's scope (else 404) and are audited.</item>
/// <item>Only the appointment's doctor opens, writes and completes the visit; vital signs may also be recorded by
/// Clinical.Vitals holders (nurses).</item>
/// <item>Every content write also updates the visit row, so its rowversion makes a write racing "complete" fail (409).
/// A completed visit never changes again; corrections are addenda.</item>
/// </list>
/// </summary>
public class VisitRepo : IVisitRepo
{
    private const long MaxAttachmentBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedFileTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
    };

    private readonly ClinicalDbContext _context;
    private readonly IMapper _mapper;
    private readonly ISchedulingApi _scheduling;
    private readonly IClinicApi _clinic;
    private readonly IAccessContext _access;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public VisitRepo(ClinicalDbContext context, IMapper mapper, ISchedulingApi scheduling, IClinicApi clinic, IAccessContext access,
        ITenantContext tenant, ICurrentUser currentUser, IFileStorage storage, IAuditService audit, TimeProvider time,
        IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _scheduling = scheduling;
        _clinic = clinic;
        _access = access;
        _tenant = tenant;
        _currentUser = currentUser;
        _storage = storage;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Open(OpenVisitModel model, CancellationToken cancellationToken)
    {
        var appointment = await _scheduling.GetAppointmentForVisitAsync(model.AppointmentId, cancellationToken);
        if (appointment is null || !_access.CanAccessBranch(appointment.BranchId))
            throw new BusinessException(_localizer["AppointmentNotFound"].Value, HttpStatusCode.NotFound);
        await EnsureVisitDoctorAsync(appointment.DoctorId, cancellationToken);

        // Opening again returns the same visit.
        var existingId = await ExistingVisitIdAsync(appointment.Id, cancellationToken);
        if (existingId is not null)
            return Created(existingId.Value);

        // Scheduling first: the appointment moves CheckedIn -> InProgress (rowversion-protected). If we crash before the
        // visit is saved, opening again finds the appointment InProgress and creates the visit.
        if (appointment.Status == AppointmentStatusEnum.CheckedIn)
            await _scheduling.StartVisitAsync(appointment.Id, cancellationToken);
        else if (appointment.Status != AppointmentStatusEnum.InProgress)
            throw new BusinessException(_localizer["AppointmentNotReadyForVisit"].Value, HttpStatusCode.BadRequest);

        var visit = new Visit
        {
            AppointmentId = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            BranchId = appointment.BranchId,
            OpenedAt = _time.GetUtcNow(),
        };
        _context.Visits.Add(visit);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains(ClinicalIndexes.VisitAppointment, StringComparison.Ordinal) == true)
        {
            // A concurrent open of the same appointment won; answer with its visit.
            _context.ChangeTracker.Clear();
            return Created((await ExistingVisitIdAsync(appointment.Id, cancellationToken))!.Value);
        }

        await _audit.LogAsync("Clinical.VisitOpened", "Visit", visit.Id, cancellationToken: cancellationToken);
        return Created(visit.Id);
    }

    public async Task<Response> Search(SearchVisitsModel model, CancellationToken cancellationToken)
    {
        var query = InScope(_context.Visits.AsNoTracking());
        if (model.PatientId is not null)
            query = query.Where(v => v.PatientId == model.PatientId);
        if (model.DoctorId is not null)
            query = query.Where(v => v.DoctorId == model.DoctorId);
        if (model.BranchId is not null)
            query = query.Where(v => v.BranchId == model.BranchId);
        if (model.Status is not null)
            query = query.Where(v => v.Status == model.Status);
        if (model.FromDate is DateOnly from)
        {
            var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(v => v.OpenedAt >= fromUtc);
        }
        if (model.ToDate is DateOnly to)
        {
            var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(v => v.OpenedAt < toUtc);
        }

        var paged = await query.OrderByDescending(v => v.OpenedAt).ThenByDescending(v => v.Id)
            .ProjectTo<VisitListDto>(_mapper.ConfigurationProvider)
            .ToPagedListAsync(model, cancellationToken);

        if (model.PatientId is not null)
            await _audit.LogAsync("Clinical.HistoryViewed", "Patient", model.PatientId, cancellationToken: cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(id, tracking: false, cancellationToken);

        var dto = _mapper.Map<VisitDto>(visit);
        var note = await _context.ClinicalNotes.AsNoTracking().FirstOrDefaultAsync(n => n.VisitId == id, cancellationToken);
        dto.Note = note is null ? null : _mapper.Map<ClinicalNoteDto>(note);
        dto.Vitals = _mapper.Map<List<VitalSignDto>>(
            await _context.VitalSigns.AsNoTracking().Where(v => v.VisitId == id).OrderBy(v => v.RecordedAt).ToListAsync(cancellationToken));
        dto.Diagnoses = _mapper.Map<List<DiagnosisDto>>(
            await _context.Diagnoses.AsNoTracking().Where(d => d.VisitId == id).OrderByDescending(d => d.IsPrimary).ThenBy(d => d.Id).ToListAsync(cancellationToken));

        var prescription = await _context.Prescriptions.AsNoTracking().FirstOrDefaultAsync(p => p.VisitId == id, cancellationToken);
        if (prescription is not null)
        {
            dto.Prescription = _mapper.Map<PrescriptionDto>(prescription);
            dto.Prescription.Items = _mapper.Map<List<PrescriptionItemDto>>(
                await _context.PrescriptionItems.AsNoTracking().Where(i => i.PrescriptionId == prescription.Id).OrderBy(i => i.Id).ToListAsync(cancellationToken));
        }

        dto.Attachments = _mapper.Map<List<VisitAttachmentDto>>(
            await _context.VisitAttachments.AsNoTracking().Where(a => a.VisitId == id).OrderBy(a => a.Id).ToListAsync(cancellationToken));
        dto.Addenda = _mapper.Map<List<ClinicalAddendumDto>>(
            await _context.ClinicalAddenda.AsNoTracking().Where(a => a.VisitId == id).OrderBy(a => a.Id).ToListAsync(cancellationToken));

        await _audit.LogAsync("Clinical.VisitViewed", "Visit", id, cancellationToken: cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: dto);
    }

    public async Task<Response> SaveNote(SaveClinicalNoteModel model, CancellationToken cancellationToken)
    {
        var visit = await LoadOpenVisitForDoctorAsync(model.VisitId, cancellationToken);

        var note = await _context.ClinicalNotes.FirstOrDefaultAsync(n => n.VisitId == visit.Id, cancellationToken);
        if (note is null)
            _context.ClinicalNotes.Add(_mapper.Map<ClinicalNote>(model));
        else
            _mapper.Map(model, note);

        await SaveVisitChangeAsync(visit, "Clinical.NoteSaved", cancellationToken);
        return Updated();
    }

    public async Task<Response> AddVitals(AddVitalSignsModel model, CancellationToken cancellationToken)
    {
        // Any Clinical.Vitals holder in the visit's branch (doctor or nurse), while the visit is open.
        var visit = await LoadVisitAsync(model.VisitId, tracking: true, cancellationToken);
        EnsureOpen(visit);

        var vital = _mapper.Map<VitalSign>(model);
        vital.RecordedByUserId = CurrentUserId();
        vital.RecordedAt = _time.GetUtcNow();
        _context.VitalSigns.Add(vital);

        await SaveVisitChangeAsync(visit, "Clinical.VitalsRecorded", cancellationToken);
        return Created(vital.Id);
    }

    public async Task<Response> SetDiagnoses(SetDiagnosesModel model, CancellationToken cancellationToken)
    {
        var visit = await LoadOpenVisitForDoctorAsync(model.VisitId, cancellationToken);

        _context.Diagnoses.RemoveRange(await _context.Diagnoses.Where(d => d.VisitId == visit.Id).ToListAsync(cancellationToken));
        foreach (var item in model.Items.DistinctBy(i => i.Icd10Code.Trim().ToUpperInvariant()))
        {
            var diagnosis = _mapper.Map<Diagnosis>(item);
            diagnosis.VisitId = visit.Id;
            _context.Diagnoses.Add(diagnosis);
        }

        await SaveVisitChangeAsync(visit, "Clinical.DiagnosesSet", cancellationToken);
        return Updated();
    }

    public async Task<Response> SetPrescription(SetPrescriptionModel model, CancellationToken cancellationToken)
    {
        var visit = await LoadOpenVisitForDoctorAsync(model.VisitId, cancellationToken);

        var prescription = await _context.Prescriptions.FirstOrDefaultAsync(p => p.VisitId == visit.Id, cancellationToken);
        if (prescription is not null)
            _context.PrescriptionItems.RemoveRange(await _context.PrescriptionItems.Where(i => i.PrescriptionId == prescription.Id).ToListAsync(cancellationToken));

        if (model.Items.Count == 0)
        {
            if (prescription is not null)
                _context.Prescriptions.Remove(prescription);
        }
        else
        {
            if (prescription is null)
            {
                prescription = new Prescription { VisitId = visit.Id };
                _context.Prescriptions.Add(prescription);
            }

            prescription.Notes = model.Notes?.Trim();
            foreach (var item in model.Items)
            {
                var row = _mapper.Map<PrescriptionItem>(item);
                row.PrescriptionId = prescription.Id;
                _context.PrescriptionItems.Add(row);
            }
        }

        await SaveVisitChangeAsync(visit, "Clinical.PrescriptionSet", cancellationToken);
        return Updated();
    }

    public async Task<Response> AddAttachment(long visitId, IFormFile file, CancellationToken cancellationToken)
    {
        var visit = await LoadOpenVisitForDoctorAsync(visitId, cancellationToken);

        var extension = Path.GetExtension(file.FileName);
        if (file.Length is <= 0 or > MaxAttachmentBytes || !AllowedFileTypes.TryGetValue(extension, out var contentType))
            throw InvalidFile();

        // The first bytes must match the type (a renamed .exe isn't a PDF).
        await using (var header = file.OpenReadStream())
        {
            var buffer = new byte[8];
            var read = await header.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
            if (!SignatureMatches(contentType, buffer.AsSpan(0, read)))
                throw InvalidFile();
        }

        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("No tenant context.");
        var path = $"tenants/{tenantId}/visits/{visit.Id}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using (var content = file.OpenReadStream())
            await _storage.SaveAsync(path, content, cancellationToken);

        var fileName = Path.GetFileName(file.FileName);
        var attachment = new VisitAttachment
        {
            VisitId = visit.Id,
            FileName = fileName.Length > 255 ? fileName[..255] : fileName,
            StoragePath = path,
            ContentType = contentType,
            SizeBytes = file.Length,
            UploadedByUserId = CurrentUserId(),
        };
        _context.VisitAttachments.Add(attachment);

        try
        {
            await SaveVisitChangeAsync(visit, "Clinical.AttachmentAdded", cancellationToken);
        }
        catch
        {
            // Don't leave an orphan file when the row couldn't be saved (e.g. the visit was completed meanwhile).
            await _storage.DeleteAsync(path, CancellationToken.None);
            throw;
        }

        return Created(attachment.Id);
    }

    public async Task<AttachmentFile> DownloadAttachment(long attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await _context.VisitAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (attachment is null || !await InScope(_context.Visits.AsNoTracking()).AnyAsync(v => v.Id == attachment.VisitId, cancellationToken))
            throw AttachmentNotFound();

        Stream content;
        try
        {
            content = await _storage.OpenReadAsync(attachment.StoragePath, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            throw AttachmentNotFound();
        }

        try
        {
            await _audit.LogAsync("Clinical.AttachmentDownloaded", "VisitAttachment", attachment.Id, cancellationToken: cancellationToken);
        }
        catch
        {
            await content.DisposeAsync();
            throw;
        }

        return new AttachmentFile(content, attachment.ContentType, attachment.FileName);
    }

    public async Task<Response> Complete(long id, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(id, tracking: true, cancellationToken);
        await EnsureVisitDoctorAsync(visit.DoctorId, cancellationToken);

        // Clinical first (the visit locks), then Scheduling. Completing again is a no-op that also retries Scheduling.
        if (visit.Complete(_time.GetUtcNow()))
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync("Clinical.VisitCompleted", "Visit", visit.Id, cancellationToken: cancellationToken);
        }

        await _scheduling.CompleteVisitAsync(visit.AppointmentId, cancellationToken);
        return Updated();
    }

    public async Task<Response> AddAddendum(AddAddendumModel model, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(model.VisitId, tracking: false, cancellationToken);
        if (visit.IsOpen)
            throw new BusinessException(_localizer["VisitStillOpen"].Value, HttpStatusCode.BadRequest);

        var addendum = _mapper.Map<ClinicalAddendum>(model);
        addendum.AuthorUserId = CurrentUserId();
        _context.ClinicalAddenda.Add(addendum);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Clinical.AddendumAdded", "Visit", visit.Id, cancellationToken: cancellationToken);
        return Created(addendum.Id);
    }

    /// <summary>Saves a change to an open visit's content together with an UPDATE of the visit row (rowversion check).</summary>
    private async Task SaveVisitChangeAsync(Visit visit, string auditAction, CancellationToken cancellationToken)
    {
        _context.Entry(visit).Property(v => v.OpenedAt).IsModified = true;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException && ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Two first-time writes of the same visit content at once (e.g. the note): the other one won.
            throw new BusinessException(_localizer["ConcurrentChange"].Value, HttpStatusCode.Conflict);
        }

        await _audit.LogAsync(auditAction, "Visit", visit.Id, cancellationToken: cancellationToken);
    }

    private async Task<Visit> LoadOpenVisitForDoctorAsync(long visitId, CancellationToken cancellationToken)
    {
        var visit = await LoadVisitAsync(visitId, tracking: true, cancellationToken);
        await EnsureVisitDoctorAsync(visit.DoctorId, cancellationToken);
        EnsureOpen(visit);
        return visit;
    }

    private async Task<Visit> LoadVisitAsync(long id, bool tracking, CancellationToken cancellationToken)
    {
        var visits = tracking ? _context.Visits : _context.Visits.AsNoTracking();
        return await InScope(visits).FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new BusinessException(_localizer["VisitNotFound"].Value, HttpStatusCode.NotFound);
    }

    private IQueryable<Visit> InScope(IQueryable<Visit> visits)
    {
        if (_access.HasAllBranches)
            return visits;

        var scope = _access.BranchIds.ToList();
        return visits.Where(v => scope.Contains(v.BranchId));
    }

    /// <summary>The signed-in member must be the doctor linked to <paramref name="doctorId"/> (ADR-010).</summary>
    private async Task EnsureVisitDoctorAsync(long doctorId, CancellationToken cancellationToken)
    {
        var myDoctorId = _access.MembershipId is long membershipId
            ? await _clinic.GetDoctorIdForMembershipAsync(membershipId, cancellationToken)
            : null;
        if (myDoctorId != doctorId)
            throw new BusinessException(_localizer["NotTheVisitDoctor"].Value, HttpStatusCode.Forbidden);
    }

    private void EnsureOpen(Visit visit)
    {
        if (!visit.IsOpen)
            throw new BusinessException(_localizer["VisitCompleted"].Value, HttpStatusCode.BadRequest);
    }

    private Task<long?> ExistingVisitIdAsync(long appointmentId, CancellationToken cancellationToken) =>
        _context.Visits.Where(v => v.AppointmentId == appointmentId).Select(v => (long?)v.Id).FirstOrDefaultAsync(cancellationToken);

    private long CurrentUserId() => _currentUser.GetUserId() ?? throw new InvalidOperationException("No signed-in user.");

    private static bool SignatureMatches(string contentType, ReadOnlySpan<byte> header) => contentType switch
    {
        "application/pdf" => header.StartsWith("%PDF"u8),
        "image/png" => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/jpeg" => header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
        _ => false,
    };

    private Response Created(long id) => new(_localizer["Created"].Value, HttpStatusCode.OK, data: id);

    private Response Updated() => new(_localizer["Updated"].Value, HttpStatusCode.OK);

    private BusinessException InvalidFile() => new(_localizer["InvalidFile"].Value, HttpStatusCode.BadRequest);

    private BusinessException AttachmentNotFound() => new(_localizer["AttachmentNotFound"].Value, HttpStatusCode.NotFound);
}
