using System.Net;
using AutoMapper;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.Enums.Billing;
using Core.Interfaces.Billing;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Billing;

/// <summary>Invoices of the patients linked to the signed-in phone (ADR-011), in the portal DTO.</summary>
public class PortalInvoiceRepo : IPortalInvoiceRepo
{
    private const int MaxInvoices = 100;

    private readonly BillingDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPatientsApi _patients;
    private readonly IAccessContext _access;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PortalInvoiceRepo(BillingDbContext context, IMapper mapper, IPatientsApi patients, IAccessContext access,
        IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _patients = patients;
        _access = access;
        _localizer = localizer;
    }

    public async Task<Response> GetMine(CancellationToken cancellationToken)
    {
        var phone = _access.PortalPhoneNumber ?? throw new InvalidOperationException("Not a portal request.");
        var patientIds = (await _patients.GetPortalPatientsAsync(phone, cancellationToken)).Select(p => p.PatientId).ToList();

        var invoices = await _context.Invoices.AsNoTracking()
            .Where(i => patientIds.Contains(i.PatientId) && i.Status != InvoiceStatusEnum.Void)
            .OrderByDescending(i => i.IssuedAt)
            .Take(MaxInvoices)
            .ToListAsync(cancellationToken);
        var ids = invoices.Select(i => i.Id).ToList();
        var items = await _context.InvoiceItems.AsNoTracking().Where(i => ids.Contains(i.InvoiceId)).OrderBy(i => i.Id).ToListAsync(cancellationToken);

        var result = invoices.Select(i =>
        {
            var dto = _mapper.Map<PortalInvoiceDto>(i);
            dto.Items = _mapper.Map<List<InvoiceItemDto>>(items.Where(x => x.InvoiceId == i.Id));
            return dto;
        }).ToList();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: result);
    }
}
