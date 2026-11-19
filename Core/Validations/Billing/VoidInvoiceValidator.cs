using Core.Models.Billing;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Billing;

public class VoidInvoiceValidator : AbstractValidator<VoidInvoiceModel>
{
    public VoidInvoiceValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage(_ => l["Required"]);
    }
}
