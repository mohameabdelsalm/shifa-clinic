using Core.Models.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Scheduling;

public class SearchAppointmentsValidator : AbstractValidator<SearchAppointmentsModel>
{
    public SearchAppointmentsValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x).Must(x => x.FromDate != default && x.ToDate >= x.FromDate && x.ToDate.DayNumber - x.FromDate.DayNumber <= 62)
            .WithMessage(_ => l["InvalidDateRange"]);
    }
}
