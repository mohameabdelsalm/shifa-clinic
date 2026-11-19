using Core.Models.Clinic;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinic;

public class SetDoctorScheduleValidator : AbstractValidator<SetDoctorScheduleModel>
{
    private static bool OnFiveMinuteGrid(TimeOnly time) => time.Second == 0 && time.Millisecond == 0 && time.Minute % 5 == 0;

    public SetDoctorScheduleValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Items).NotNull().WithMessage(_ => l["Required"]);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DayOfWeek).IsInEnum().WithMessage(_ => l["InvalidInput"]);
            item.RuleFor(i => i.SlotMinutes).InclusiveBetween(5, 240).Must(m => m % 5 == 0).WithMessage(_ => l["InvalidSchedule"]);
            // ADR-008: bookings claim 5-minute ticks, so schedule boundaries must sit on the 5-minute grid.
            item.RuleFor(i => i).Must(i => OnFiveMinuteGrid(i.StartTime) && OnFiveMinuteGrid(i.EndTime)).WithMessage(_ => l["InvalidSchedule"]);
            item.RuleFor(i => i).Must(i => i.EndTime > i.StartTime).WithMessage(_ => l["InvalidSchedule"]);
            item.RuleFor(i => i).Must(i => i.SlotMinutes > 0 && (i.EndTime - i.StartTime).TotalMinutes % i.SlotMinutes == 0)
                .WithMessage(_ => l["InvalidSchedule"]);
        });
    }
}
