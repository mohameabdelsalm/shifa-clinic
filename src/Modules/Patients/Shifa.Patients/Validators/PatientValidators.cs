using FluentValidation;
using Microsoft.Extensions.Localization;
using Shifa.Core;
using Shifa.Core.Common;
using Shifa.Patients.Models;

namespace Shifa.Patients.Validators;

internal static class PatientRules
{
    public static bool IsPlausibleBirthDate(DateOnly date) =>
        date >= new DateOnly(1900, 1, 1) && date <= DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool IsValidOptionalPhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) || PhoneNumbers.Normalize(phone) is not null;
}

public class PatientIdentifierValidator : AbstractValidator<PatientIdentifierModel>
{
    public PatientIdentifierValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
    }
}

public class PatientGuardianModelValidator : AbstractValidator<PatientGuardianModel>
{
    public PatientGuardianModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Relationship).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PhoneNumber).Must(PatientRules.IsValidOptionalPhone).WithMessage(_ => l["InvalidPhone"]);
    }
}

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

public class UpdatePatientValidator : AbstractValidator<UpdatePatientModel>
{
    public UpdatePatientValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DateOfBirth).Must(PatientRules.IsPlausibleBirthDate).WithMessage(_ => l["InvalidDateOfBirth"]);
        RuleFor(x => x.Gender).IsInEnum().WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.PhoneNumber).Must(PatientRules.IsValidOptionalPhone).WithMessage(_ => l["InvalidPhone"]);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage(_ => l["InvalidEmail"]);
        RuleFor(x => x.Address).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
        RuleForEach(x => x.Identifiers).SetValidator(new PatientIdentifierValidator(l));
    }
}

public class AddGuardianValidator : AbstractValidator<AddGuardianModel>
{
    public AddGuardianValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Relationship).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PhoneNumber).Must(PatientRules.IsValidOptionalPhone).WithMessage(_ => l["InvalidPhone"]);
    }
}

public class GrantConsentValidator : AbstractValidator<GrantConsentModel>
{
    public GrantConsentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.ConsentType).IsInEnum().WithMessage(_ => l["InvalidInput"]);
    }
}

public class LinkPortalAccessValidator : AbstractValidator<LinkPortalAccessModel>
{
    public LinkPortalAccessValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.PhoneNumber).Must(p => PhoneNumbers.Normalize(p) is not null).WithMessage(_ => l["InvalidPhone"]);
        RuleFor(x => x.Relationship).IsInEnum().WithMessage(_ => l["InvalidInput"]);
    }
}
