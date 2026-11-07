using Core.Models.Clinical;
using Core.Validations.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

public class SearchVisitsValidator : AbstractValidator<SearchVisitsModel>
{
    public SearchVisitsValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x)
            .Must(x => (x.FromDate is null || SchedulingDateRules.IsSaneDate(x.FromDate.Value))
                       && (x.ToDate is null || SchedulingDateRules.IsSaneDate(x.ToDate.Value))
                       && (x.FromDate is null || x.ToDate is null || x.ToDate >= x.FromDate))
            .WithMessage(_ => l["InvalidDateRange"]);
    }
}
