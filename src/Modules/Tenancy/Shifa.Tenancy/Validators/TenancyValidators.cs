using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Shifa.Core;
using Shifa.Tenancy.Contracts;
using Shifa.Tenancy.Models;

namespace Shifa.Tenancy.Validators;

internal static partial class TenancyRules
{
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    public static partial Regex Slug();

    public static bool IsValidEntitlementCode(string code) =>
        code is EntitlementCodes.MaxBranches or EntitlementCodes.MaxDoctors or EntitlementCodes.MaxEmployees
        || (code.StartsWith(EntitlementCodes.ModulePrefix, StringComparison.Ordinal) && code.Length > EntitlementCodes.ModulePrefix.Length && code.Length <= 100);

    public static bool IsValidTimeZone(string? id)
    {
        if (id is null)
            return true;
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}

public class AddPlanValidator : AbstractValidator<AddPlanModel>
{
    public AddPlanValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.TrialDays).InclusiveBetween(0, 365).WithMessage(_ => l["InvalidInput"]);
        RuleForEach(x => x.Entitlements).Must(e => TenancyRules.IsValidEntitlementCode(e.Code)).WithMessage(_ => l["InvalidInput"]);
        RuleForEach(x => x.Entitlements).Must(e => e.LimitValue is null or >= 0).WithMessage(_ => l["InvalidInput"]);
    }
}

public class UpdatePlanValidator : AbstractValidator<UpdatePlanModel>
{
    public UpdatePlanValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.TrialDays).InclusiveBetween(0, 365).WithMessage(_ => l["InvalidInput"]);
        RuleForEach(x => x.Entitlements).Must(e => TenancyRules.IsValidEntitlementCode(e.Code)).WithMessage(_ => l["InvalidInput"]);
        RuleForEach(x => x.Entitlements).Must(e => e.LimitValue is null or >= 0).WithMessage(_ => l["InvalidInput"]);
    }
}

public class AddTenantValidator : AbstractValidator<AddTenantModel>
{
    public AddTenantValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"])
            .Must(s => TenancyRules.Slug().IsMatch(s)).WithMessage(_ => l["InvalidSlug"]);
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DefaultTimeZoneId).Must(TenancyRules.IsValidTimeZone).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.AdminFullName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.AdminEmail).NotEmpty().MaximumLength(256).EmailAddress().WithMessage(_ => l["InvalidEmail"]);
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(10).MaximumLength(128)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").WithMessage(_ => l["PasswordTooWeak"]);
    }
}

public class ChangePlanValidator : AbstractValidator<ChangePlanModel>
{
    public ChangePlanValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage(_ => l["Required"]);
    }
}

public class RecordPaymentValidator : AbstractValidator<RecordPaymentModel>
{
    public RecordPaymentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Reference).MaximumLength(200).WithMessage(_ => l["InvalidInput"]);
    }
}
