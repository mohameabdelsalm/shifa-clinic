using Core.DTO.Tenancy;
using Core.Models.Tenancy;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Tenancy;

public class SetTenantFeatureValidator : AbstractValidator<SetTenantFeatureModel>
{
    public SetTenantFeatureValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Code).Must(FeatureCodes.IsKnown).WithMessage(_ => l["InvalidInput"]);
    }
}
