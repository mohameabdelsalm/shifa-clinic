using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class LoginModelValidator : AbstractValidator<LoginModel>
{
    public LoginModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(256).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128).WithMessage(_ => l["Required"]);
        RuleFor(x => x.TenantSlug).MaximumLength(100).WithMessage(_ => l["InvalidSlug"]);
    }
}
