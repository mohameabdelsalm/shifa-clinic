using Core.Models.Tenancy;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Tenancy;

public class AddTenantValidator : AbstractValidator<AddTenantModel>
{
    public AddTenantValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"])
            .Must(s => TenancyRules.Slug().IsMatch(s)).WithMessage(_ => l["InvalidSlug"]);
        RuleFor(x => x.PlanId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DefaultTimeZoneId).Must(TenancyRules.IsValidTimeZone).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.AdminFullName).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.AdminEmail).NotEmpty().MaximumLength(256).EmailAddress().WithMessage(_ => l["InvalidEmail"]);
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(10).MaximumLength(128)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").WithMessage(_ => l["PasswordTooWeak"]);
    }
}
