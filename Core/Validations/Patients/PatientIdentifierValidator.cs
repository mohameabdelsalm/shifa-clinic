using Core.Models.Patients;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Patients;

public class PatientIdentifierValidator : AbstractValidator<PatientIdentifierModel>
{
    public PatientIdentifierValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
    }
}
