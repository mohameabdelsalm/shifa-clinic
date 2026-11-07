using Core.Models.Clinic;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinic;

public class UpdateServiceValidator : AbstractValidator<UpdateServiceModel>
{
    public UpdateServiceValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480).WithMessage(_ => l["InvalidInput"]);
    }
}
