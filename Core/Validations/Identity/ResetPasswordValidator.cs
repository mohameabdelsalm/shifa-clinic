using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordModel>
{
    public ResetPasswordValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.NewPassword).StrongPassword(l);
    }
}
