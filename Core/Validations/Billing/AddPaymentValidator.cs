using Core.Enums.Billing;
using Core.Models.Billing;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Billing;

public class AddPaymentValidator : AbstractValidator<AddPaymentModel>
{
    public AddPaymentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.InvoiceId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Amount).Must(MoneyRules.IsValidAmount).WithMessage(_ => l["InvalidAmount"]);

        // Online payments are recorded only by the gateway callback.
        RuleFor(x => x.Method).Must(m => m is PaymentMethodEnum.Cash or PaymentMethodEnum.Card or PaymentMethodEnum.Wallet)
            .WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(100)
            .When(x => x.Method is PaymentMethodEnum.Card or PaymentMethodEnum.Wallet)
            .WithMessage(_ => l["PaymentReferenceRequired"]);
        RuleFor(x => x.Reference).MaximumLength(100).WithMessage(_ => l["InvalidInput"]);
    }
}
