using Core.Models.Clinic;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinic;

public class UpdateBranchValidator : AbstractValidator<UpdateBranchModel>
{
    public UpdateBranchValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
        RuleFor(x => x.TimeZoneId).Must(ClinicRules.IsValidTimeZone).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Phone).MaximumLength(30).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Address).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}
