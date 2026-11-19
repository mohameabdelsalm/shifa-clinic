using Core.Models.Patients;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Patients;

public class GrantConsentValidator : AbstractValidator<GrantConsentModel>
{
    public GrantConsentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.ConsentType).IsInEnum().WithMessage(_ => l["InvalidInput"]);
    }
}
