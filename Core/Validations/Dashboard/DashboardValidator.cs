using Core.Models.Dashboard;
using Core.Validations.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Dashboard;

public class DashboardValidator : AbstractValidator<DashboardModel>
{
    public DashboardValidator(IStringLocalizer<LanguageResource> l)
    {
        // At most about a year, so one request stays cheap.
        RuleFor(x => x)
            .Must(x => (x.DateFrom is null || SchedulingDateRules.IsSaneDate(x.DateFrom.Value))
                       && (x.DateTo is null || SchedulingDateRules.IsSaneDate(x.DateTo.Value))
                       && (x.DateFrom is null || x.DateTo is null
                           || (x.DateTo >= x.DateFrom && x.DateTo.Value.DayNumber - x.DateFrom.Value.DayNumber <= 366)))
            .WithMessage(_ => l["InvalidDateRange"]);
        RuleFor(x => x.BranchIds).Must(ids => ids is null || ids.Count <= 100).WithMessage(_ => l["InvalidInput"]);
    }
}
