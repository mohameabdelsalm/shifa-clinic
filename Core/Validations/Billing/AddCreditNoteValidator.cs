using Core.Models.Billing;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Billing;

public class AddCreditNoteValidator : AbstractValidator<AddCreditNoteModel>
{
    public AddCreditNoteValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.InvoiceId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Amount).Must(MoneyRules.IsValidAmount).WithMessage(_ => l["InvalidAmount"]);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage(_ => l["Required"]);
    }
}
