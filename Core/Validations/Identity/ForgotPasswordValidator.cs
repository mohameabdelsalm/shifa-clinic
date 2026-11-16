using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordModel>
{
    public ForgotPasswordValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Email).ValidEmail(l);
    }
}
