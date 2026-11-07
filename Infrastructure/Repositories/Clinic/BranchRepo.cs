using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Clinic;
using Core.DTO.Tenancy;
using Core.Entities.Clinic;
using Core.Enums.Common;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Interfaces.Tenancy;
using Core.Models.Clinic;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Clinic;

/// <summary>
/// Branches of the current tenant. A member limited to some branches sees and edits only those;
/// a branch outside their scope is "not found". Creating a branch is an organization-level action,
/// so it needs access to all branches. The plan's MaxBranches limit applies.
/// </summary>
public class BranchRepo : IBranchRepo
{
    private readonly ClinicDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;
    private readonly IAccessContext _access;
    private readonly ITenancyApi _tenancy;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public BranchRepo(ClinicDbContext context, IMapper mapper, ITenantContext tenantContext, IAccessContext access,
        ITenancyApi tenancy, IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _tenantContext = tenantContext;
        _access = access;
        _tenancy = tenancy;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> GetAll(CancellationToken cancellationToken)
    {
        var branches = await ProjectBranches(InScope(_context.Branches.AsNoTracking().Where(b => b.Status == StatusEnum.Active)))
            .OrderBy(b => b.Name).ToListAsync(cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: branches);
    }

    public async Task<Response> Search(SearchBranchesModel model, CancellationToken cancellationToken)
    {
        var query = InScope(_context.Branches.AsNoTracking());
        if (!string.IsNullOrWhiteSpace(model.Text))
        {
            var text = model.Text.Trim();
            query = query.Where(b => b.Name.Contains(text) || b.Code.Contains(text));
        }

        var paged = await ProjectBranches(query).OrderBy(b => b.Name).ToPagedListAsync(model, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var branch = await ProjectBranches(InScope(_context.Branches.AsNoTracking()).Where(b => b.Id == id)).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: branch);
    }

    public async Task<Response> Add(AddBranchModel model, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("No tenant context.");
        if (!_access.HasAllBranches)
            throw new BusinessException(_localizer["Forbidden"].Value, HttpStatusCode.Forbidden);

        var limit = await _tenancy.GetEntitlementAsync(tenantId, EntitlementCodes.MaxBranches, cancellationToken);
        if (!limit.Enabled ||
            (limit.Limit is int max && await _context.Branches.CountAsync(b => b.Status == StatusEnum.Active, cancellationToken) >= max))
            throw new BusinessException(_localizer["BranchLimitReached"].Value, HttpStatusCode.BadRequest);

        var code = model.Code.Trim().ToUpperInvariant();
        var name = model.Name.Trim();
        await EnsureUniqueAsync(null, code, name, cancellationToken);

        var branch = _mapper.Map<Branch>(model);
        _context.Branches.Add(branch);

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Branches.Created", "Branch", branch.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: branch.Id);
    }

    public async Task<Response> Update(UpdateBranchModel model, CancellationToken cancellationToken)
    {
        var branch = await InScope(_context.Branches).FirstOrDefaultAsync(b => b.Id == model.Id, cancellationToken) ?? throw NotFound();

        var code = model.Code.Trim().ToUpperInvariant();
        var name = model.Name.Trim();
        await EnsureUniqueAsync(branch.Id, code, name, cancellationToken);

        _mapper.Map(model, branch);

        await _context.SaveChangesAsync(cancellationToken);
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken)
    {
        var branch = await InScope(_context.Branches).FirstOrDefaultAsync(b => b.Id == id, cancellationToken) ?? throw NotFound();
        branch.Status = status;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync($"Branches.{status}", "Branch", id, cancellationToken: cancellationToken);
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    private IQueryable<Branch> InScope(IQueryable<Branch> branches)
    {
        if (_access.HasAllBranches)
            return branches;

        var scope = _access.BranchIds.ToList();
        return branches.Where(b => scope.Contains(b.Id));
    }

    private async Task EnsureUniqueAsync(long? ignoreId, string code, string name, CancellationToken cancellationToken)
    {
        if (await _context.Branches.AnyAsync(b => b.Id != ignoreId && (b.Code == code || b.Name == name), cancellationToken))
            throw new BusinessException(_localizer["DuplicateName"].Value, HttpStatusCode.BadRequest);
    }

    private IQueryable<BranchDto> ProjectBranches(IQueryable<Branch> branches) =>
        branches.ProjectTo<BranchDto>(_mapper.ConfigurationProvider);

    private BusinessException NotFound() => new(_localizer["BranchNotFound"].Value, HttpStatusCode.NotFound);
}
