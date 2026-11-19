using Core.Models.Clinical;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

public class SaveClinicalNoteValidator : AbstractValidator<SaveClinicalNoteModel>
{
    public SaveClinicalNoteValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.VisitId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.ChiefComplaint).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Notes).MaximumLength(4000).WithMessage(_ => l["InvalidInput"]);
    }
}
