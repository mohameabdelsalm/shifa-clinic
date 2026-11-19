using Core.Models.Clinical;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

public class SetPrescriptionValidator : AbstractValidator<SetPrescriptionModel>
{
    public SetPrescriptionValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.VisitId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Items).Must(items => items.Count <= 30 && items.All(i => i is not null)).WithMessage(_ => l["InvalidInput"]);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DrugName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
            item.RuleFor(i => i.Dosage).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
            item.RuleFor(i => i.Frequency).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
            item.RuleFor(i => i.DurationDays).InclusiveBetween(1, 365).WithMessage(_ => l["InvalidInput"]);
            item.RuleFor(i => i.Instructions).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
        });
    }
}
