using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class SwitchTenantModelValidator : AbstractValidator<SwitchTenantModel>
{
    public SwitchTenantModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
    }
}
