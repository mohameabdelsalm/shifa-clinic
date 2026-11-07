using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class RefreshTokenModelValidator : AbstractValidator<RefreshTokenModel>
{
    public RefreshTokenModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
    }
}
