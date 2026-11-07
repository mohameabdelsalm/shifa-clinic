using FluentValidation;
using Microsoft.Extensions.Localization;
using Shifa.Core;
using Shifa.Core.Common;
using Shifa.Identity.Models;

namespace Shifa.Identity.Validators;

/// <summary>Shared password and email rules.</summary>
public static class ValidationRules
{
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule, IStringLocalizer<LanguageResource> l)
        => rule.NotEmpty().WithMessage(_ => l["Required"])
            .MinimumLength(10).WithMessage(_ => l["PasswordTooWeak"])
            .MaximumLength(128).WithMessage(_ => l["PasswordTooWeak"])
            .Matches("[A-Z]").WithMessage(_ => l["PasswordTooWeak"])
            .Matches("[a-z]").WithMessage(_ => l["PasswordTooWeak"])
            .Matches("[0-9]").WithMessage(_ => l["PasswordTooWeak"]);

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule, IStringLocalizer<LanguageResource> l)
        => rule.NotEmpty().WithMessage(_ => l["Required"])
            .MaximumLength(256).WithMessage(_ => l["InvalidEmail"])
            .EmailAddress().WithMessage(_ => l["InvalidEmail"]);
}

public class LoginModelValidator : AbstractValidator<LoginModel>
{
    public LoginModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(256).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128).WithMessage(_ => l["Required"]);
        RuleFor(x => x.TenantSlug).MaximumLength(100).WithMessage(_ => l["InvalidSlug"]);
    }
}

public class RequestOtpModelValidator : AbstractValidator<RequestOtpModel>
{
    public RequestOtpModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PhoneNumber).Must(p => PhoneNumbers.Normalize(p) is not null).WithMessage(_ => l["InvalidPhone"]);
    }
}

public class VerifyOtpModelValidator : AbstractValidator<VerifyOtpModel>
{
    public VerifyOtpModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PhoneNumber).Must(p => PhoneNumbers.Normalize(p) is not null).WithMessage(_ => l["InvalidPhone"]);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$").WithMessage(_ => l["InvalidOtp"]);
    }
}

public class RefreshTokenModelValidator : AbstractValidator<RefreshTokenModel>
{
    public RefreshTokenModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
    }
}

public class SwitchTenantModelValidator : AbstractValidator<SwitchTenantModel>
{
    public SwitchTenantModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
    }
}

public class AddRoleValidator : AbstractValidator<AddRoleModel>
{
    public AddRoleValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Permissions).NotNull().WithMessage(_ => l["Required"]);
        RuleFor(x => x.Permissions).Must(p => p is null || p.All(Permissions.All.Contains)).WithMessage(_ => l["InvalidPermission"]);
    }
}

public class UpdateRoleValidator : AbstractValidator<UpdateRoleModel>
{
    public UpdateRoleValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Permissions).NotNull().WithMessage(_ => l["Required"]);
        RuleFor(x => x.Permissions).Must(p => p is null || p.All(Permissions.All.Contains)).WithMessage(_ => l["InvalidPermission"]);
    }
}

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

public class UpdateStaffValidator : AbstractValidator<UpdateStaffModel>
{
    public UpdateStaffValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage(_ => l["Required"]);
    }
}
