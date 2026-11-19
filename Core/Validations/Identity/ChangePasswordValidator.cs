using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordModel>
{
    public ChangePasswordValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128).WithMessage(_ => l["Required"]);
        RuleFor(x => x.NewPassword).StrongPassword(l)
            .NotEqual(x => x.CurrentPassword).WithMessage(_ => l["PasswordMustChange"]);
    }
}
