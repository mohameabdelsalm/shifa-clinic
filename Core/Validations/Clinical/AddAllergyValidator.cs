using Core.Models.Clinical;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

public class AddAllergyValidator : AbstractValidator<AddAllergyModel>
{
    public AddAllergyValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Allergen).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Severity).IsInEnum().WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Reaction).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}
