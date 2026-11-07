using FluentValidation;
using Microsoft.Extensions.Localization;
using Shifa.Core;
using Shifa.Scheduling.Enums;
using Shifa.Scheduling.Models;

namespace Shifa.Scheduling.Validators;

public class AvailabilityValidator : AbstractValidator<AvailabilityModel>
{
    public AvailabilityValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.ServiceId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage(_ => l["Required"]);
    }
}

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

public class PortalBookAppointmentValidator : AbstractValidator<PortalBookAppointmentModel>
{
    public PortalBookAppointmentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.ServiceId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.StartAt).NotEqual(default(DateTimeOffset)).WithMessage(_ => l["Required"]);
    }
}

public class RescheduleAppointmentValidator : AbstractValidator<RescheduleAppointmentModel>
{
    public RescheduleAppointmentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.StartAt).NotEqual(default(DateTimeOffset)).WithMessage(_ => l["Required"]);
    }
}

public class CancelAppointmentValidator : AbstractValidator<CancelAppointmentModel>
{
    public CancelAppointmentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}

public class SearchAppointmentsValidator : AbstractValidator<SearchAppointmentsModel>
{
    public SearchAppointmentsValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x).Must(x => x.FromDate != default && x.ToDate >= x.FromDate && x.ToDate.DayNumber - x.FromDate.DayNumber <= 62)
            .WithMessage(_ => l["InvalidDateRange"]);
    }
}
