using Core.Models.Clinic;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinic;

public class UpdateDoctorValidator : AbstractValidator<UpdateDoctorModel>
{
    public UpdateDoctorValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullNameEn).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Title).MaximumLength(100).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.SpecialtyIds).NotEmpty().WithMessage(_ => l["Required"]);
    }
}
