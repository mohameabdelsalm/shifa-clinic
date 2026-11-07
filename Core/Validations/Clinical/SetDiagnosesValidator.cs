using Core.Models.Clinical;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

public class SetDiagnosesValidator : AbstractValidator<SetDiagnosesModel>
{
    public SetDiagnosesValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.VisitId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Items).Must(items => items.Count <= 20).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Items).Must(items => items.Count(i => i.IsPrimary) <= 1).WithMessage(_ => l["OnePrimaryDiagnosis"]);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Icd10Code).Must(ClinicalRules.IsValidIcd10).WithMessage(_ => l["InvalidIcd10"]);
            item.RuleFor(i => i.Description).MaximumLength(300).WithMessage(_ => l["InvalidInput"]);
        });
    }
}
