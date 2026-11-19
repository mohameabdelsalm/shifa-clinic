using Core.Models.Clinic;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinic;

public class LinkDoctorAccountValidator : AbstractValidator<LinkDoctorAccountModel>
{
    public LinkDoctorAccountValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.MembershipId).GreaterThan(0).When(x => x.MembershipId is not null).WithMessage(_ => l["InvalidInput"]);
    }
}
