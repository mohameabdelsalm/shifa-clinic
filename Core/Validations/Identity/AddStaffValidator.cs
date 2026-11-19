using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class AddStaffValidator : AbstractValidator<AddStaffModel>
{
    public AddStaffValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Email).ValidEmail(l);
        RuleFor(x => x.Password!).StrongPassword(l).When(x => x.Password is not null);
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage(_ => l["Required"]);
    }
}
