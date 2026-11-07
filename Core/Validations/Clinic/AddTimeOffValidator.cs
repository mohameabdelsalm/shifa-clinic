using Core.Models.Clinic;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinic;

public class AddTimeOffValidator : AbstractValidator<AddTimeOffModel>
{
    public AddTimeOffValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.ToAt).GreaterThan(x => x.FromAt).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}
