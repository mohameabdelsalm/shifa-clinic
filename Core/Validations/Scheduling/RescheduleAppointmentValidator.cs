using Core.Models.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Scheduling;

public class RescheduleAppointmentValidator : AbstractValidator<RescheduleAppointmentModel>
{
    public RescheduleAppointmentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.StartAt).NotEqual(default(DateTimeOffset)).WithMessage(_ => l["Required"]);
    }
}
