using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class UpdateStaffValidator : AbstractValidator<UpdateStaffModel>
{
    public UpdateStaffValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage(_ => l["Required"]);
    }
}
