using Core.Models.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Scheduling;

public class CancelAppointmentValidator : AbstractValidator<CancelAppointmentModel>
{
    public CancelAppointmentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}
