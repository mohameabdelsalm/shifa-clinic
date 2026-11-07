using Core.Models.Clinical;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

public class OpenVisitValidator : AbstractValidator<OpenVisitModel>
{
    public OpenVisitValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.AppointmentId).GreaterThan(0).WithMessage(_ => l["Required"]);
    }
}
