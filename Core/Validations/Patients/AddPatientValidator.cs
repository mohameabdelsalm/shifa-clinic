using Core.Models.Patients;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Patients;

public class AddPatientValidator : AbstractValidator<AddPatientModel>
{
    public AddPatientValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DateOfBirth).Must(PatientRules.IsPlausibleBirthDate).WithMessage(_ => l["InvalidDateOfBirth"]);
        RuleFor(x => x.Gender).IsInEnum().WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.PhoneNumber).Must(PatientRules.IsValidOptionalPhone).WithMessage(_ => l["InvalidPhone"]);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage(_ => l["InvalidEmail"]);
        RuleFor(x => x.Address).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
        RuleForEach(x => x.Identifiers).SetValidator(new PatientIdentifierValidator(l));
        RuleForEach(x => x.Guardians).SetValidator(new PatientGuardianModelValidator(l));
    }
}
