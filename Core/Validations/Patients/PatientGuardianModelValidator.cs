using Core.Models.Patients;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Patients;

public class PatientGuardianModelValidator : AbstractValidator<PatientGuardianModel>
{
    public PatientGuardianModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Relationship).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PhoneNumber).Must(PatientRules.IsValidOptionalPhone).WithMessage(_ => l["InvalidPhone"]);
    }
}
