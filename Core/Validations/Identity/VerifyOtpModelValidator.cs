using Core.Common;
using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class VerifyOtpModelValidator : AbstractValidator<VerifyOtpModel>
{
    public VerifyOtpModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PhoneNumber).Must(p => PhoneNumbers.Normalize(p) is not null).WithMessage(_ => l["InvalidPhone"]);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$").WithMessage(_ => l["InvalidOtp"]);
    }
}
