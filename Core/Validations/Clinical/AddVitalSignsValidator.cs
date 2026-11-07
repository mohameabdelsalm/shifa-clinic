using Core.Models.Clinical;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Clinical;

/// <summary>At least one measurement, each within a plausible human range (catches typos like 1200 bpm).</summary>
public class AddVitalSignsValidator : AbstractValidator<AddVitalSignsModel>
{
    public AddVitalSignsValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.VisitId).GreaterThan(0).WithMessage(_ => l["Required"]);
        RuleFor(x => x)
            .Must(x => x.BpSystolic is not null || x.BpDiastolic is not null || x.HeartRate is not null || x.TemperatureC is not null
                       || x.WeightKg is not null || x.HeightCm is not null || x.OxygenSaturation is not null)
            .WithMessage(_ => l["VitalsRequired"]);

        RuleFor(x => x.BpSystolic).InclusiveBetween(50, 260).When(x => x.BpSystolic is not null).WithMessage(_ => l["VitalsRequired"]);
        RuleFor(x => x.BpDiastolic).InclusiveBetween(30, 160).When(x => x.BpDiastolic is not null).WithMessage(_ => l["VitalsRequired"]);
        RuleFor(x => x.HeartRate).InclusiveBetween(20, 250).When(x => x.HeartRate is not null).WithMessage(_ => l["VitalsRequired"]);
        RuleFor(x => x.TemperatureC).InclusiveBetween(30m, 45m).When(x => x.TemperatureC is not null).WithMessage(_ => l["VitalsRequired"]);
        RuleFor(x => x.WeightKg).InclusiveBetween(0.3m, 400m).When(x => x.WeightKg is not null).WithMessage(_ => l["VitalsRequired"]);
        RuleFor(x => x.HeightCm).InclusiveBetween(20m, 250m).When(x => x.HeightCm is not null).WithMessage(_ => l["VitalsRequired"]);
        RuleFor(x => x.OxygenSaturation).InclusiveBetween(50, 100).When(x => x.OxygenSaturation is not null).WithMessage(_ => l["VitalsRequired"]);
    }
}
