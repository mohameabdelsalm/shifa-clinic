using Core.Models.Clinical;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

public class AddAddendumValidator : AbstractValidator<AddAddendumModel>
{
    public AddAddendumValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.VisitId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000).WithMessage(_ => l["Required"]);
    }
}
