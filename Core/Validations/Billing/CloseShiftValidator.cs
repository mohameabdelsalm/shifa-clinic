using Core.Models.Billing;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Billing;

public class CloseShiftValidator : AbstractValidator<CloseShiftModel>
{
    public CloseShiftValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.ShiftId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.CountedCash).Must(a => a >= 0 && a <= MoneyRules.MaxAmount && MoneyRules.HasTwoDecimalsAtMost(a)).WithMessage(_ => l["InvalidAmount"]);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}
