using Core.Common;
using Core.Models.Patients;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Patients;

public class LinkPortalAccessValidator : AbstractValidator<LinkPortalAccessModel>
{
    public LinkPortalAccessValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.PhoneNumber).Must(p => PhoneNumbers.Normalize(p) is not null).WithMessage(_ => l["InvalidPhone"]);
        RuleFor(x => x.Relationship).IsInEnum().WithMessage(_ => l["InvalidInput"]);
    }
}
