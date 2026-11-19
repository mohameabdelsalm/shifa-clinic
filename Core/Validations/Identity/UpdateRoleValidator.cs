using Core.Common;
using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class UpdateRoleValidator : AbstractValidator<UpdateRoleModel>
{
    public UpdateRoleValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Permissions).NotNull().WithMessage(_ => l["Required"]);
        RuleFor(x => x.Permissions).Must(p => p is null || p.All(Permissions.All.Contains)).WithMessage(_ => l["InvalidPermission"]);
    }
}
