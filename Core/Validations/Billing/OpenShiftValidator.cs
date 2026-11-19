using Core.Models.Billing;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Billing;

public class OpenShiftValidator : AbstractValidator<OpenShiftModel>
{
    public OpenShiftValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.OpeningCash).Must(a => a >= 0 && a <= MoneyRules.MaxAmount && MoneyRules.HasTwoDecimalsAtMost(a)).WithMessage(_ => l["InvalidAmount"]);
    }
}
