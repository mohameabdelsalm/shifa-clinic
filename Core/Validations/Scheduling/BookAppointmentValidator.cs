using Core.Enums.Scheduling;
using Core.Models.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Scheduling;

public class BookAppointmentValidator : AbstractValidator<BookAppointmentModel>
{
    public BookAppointmentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.ServiceId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.StartAt).NotEqual(default(DateTimeOffset)).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Source).Must(s => s is AppointmentSourceEnum.Reception or AppointmentSourceEnum.Phone).WithMessage(_ => l["InvalidInput"]);
    }
}
