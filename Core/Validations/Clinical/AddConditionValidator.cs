using Core.Models.Clinical;
using Core.Validations.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

public class AddConditionValidator : AbstractValidator<AddConditionModel>
{
    public AddConditionValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Condition).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Since).Must(d => SchedulingDateRules.IsSaneDate(d!.Value)).When(x => x.Since is not null).WithMessage(_ => l["InvalidDateRange"]);
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}
