using Core.Models.Clinic;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinic;

public class SetServicePriceValidator : AbstractValidator<SetServicePriceModel>
{
    public SetServicePriceValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, false).WithMessage(_ => l["InvalidInput"]);
    }
}
