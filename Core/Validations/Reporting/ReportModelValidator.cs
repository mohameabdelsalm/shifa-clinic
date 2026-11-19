using Core.Models.Reporting;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Reporting;

public class ReportModelValidator : AbstractValidator<ReportModel>
{
    public const int MaxDays = 366;

    public ReportModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.DateFrom).NotEmpty().WithMessage(_ => l["Required"]);
        RuleFor(x => x.DateTo).NotEmpty().WithMessage(_ => l["Required"])
            .GreaterThanOrEqualTo(x => x.DateFrom).WithMessage(_ => l["InvalidDateRange"])
            .Must((m, to) => to.DayNumber - m.DateFrom.DayNumber < MaxDays).WithMessage(_ => l["ReportPeriodTooLong"]);
        RuleFor(x => x.Format).IsInEnum().WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.BranchIds).Must(ids => ids is null || ids.Count <= 50).WithMessage(_ => l["InvalidInput"]);
    }
}
