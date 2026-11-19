using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

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
