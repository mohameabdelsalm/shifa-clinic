using Core.Models.Tenancy;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Tenancy;

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
