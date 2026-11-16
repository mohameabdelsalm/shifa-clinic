using Core.Models.Billing;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Billing;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceModel>
{
    public CreateInvoiceValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Items)
            .Must(items => items.Count is >= 1 and <= 20 && items.All(i => i is not null)
                           && items.Select(i => i.ServiceId).Distinct().Count() == items.Count)
            .WithMessage(_ => l["InvalidInput"]);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServiceId).GreaterThan(0).WithMessage(_ => l["Required"]);
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 100).WithMessage(_ => l["InvalidInput"]);
        });
    }
}
