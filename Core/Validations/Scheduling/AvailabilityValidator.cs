using Core.Models.Scheduling;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Scheduling;

public class AvailabilityValidator : AbstractValidator<AvailabilityModel>
{
    public AvailabilityValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.ServiceId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage(_ => l["Required"]);
    }
}
