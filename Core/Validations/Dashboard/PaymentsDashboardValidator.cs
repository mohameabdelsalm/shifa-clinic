using Core.Models.Dashboard;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Dashboard;

public class PaymentsDashboardValidator : AbstractValidator<PaymentsDashboardModel>
{
    public PaymentsDashboardValidator(IStringLocalizer<LanguageResource> l)
    {
        Include(new DashboardValidator(l));
    }
}
