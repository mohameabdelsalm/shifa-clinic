using Core.Models.Billing;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Billing;

public class SavePaymentGatewayValidator : AbstractValidator<SavePaymentGatewayModel>
{
    public SavePaymentGatewayValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.PublicKey).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.SecretKey).NotEmpty().MaximumLength(300).WithMessage(_ => l["Required"]);
        RuleFor(x => x.HmacSecret).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.IntegrationIds).Must(ids => ids.Count is >= 1 and <= 10 && ids.All(id => id > 0)).WithMessage(_ => l["InvalidInput"]);
    }
}
