using Core.Models.Tenancy;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Tenancy;

public class ChangePlanValidator : AbstractValidator<ChangePlanModel>
{
    public ChangePlanValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage(_ => l["Required"]);
    }
}
