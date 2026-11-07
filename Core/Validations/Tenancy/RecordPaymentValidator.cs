using Core.Models.Tenancy;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Tenancy;

public class RecordPaymentValidator : AbstractValidator<RecordPaymentModel>
{
    public RecordPaymentValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.TenantId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Reference).MaximumLength(200).WithMessage(_ => l["InvalidInput"]);
    }
}
