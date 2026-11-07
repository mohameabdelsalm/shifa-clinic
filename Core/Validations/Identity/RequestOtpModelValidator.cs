using Core.Common;
using Core.Models.Identity;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Identity;

public class RequestOtpModelValidator : AbstractValidator<RequestOtpModel>
{
    public RequestOtpModelValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(100).WithMessage(_ => l["Required"]);
        RuleFor(x => x.PhoneNumber).Must(p => PhoneNumbers.Normalize(p) is not null).WithMessage(_ => l["InvalidPhone"]);
    }
}
