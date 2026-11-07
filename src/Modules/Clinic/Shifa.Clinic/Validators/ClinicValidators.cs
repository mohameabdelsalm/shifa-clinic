using FluentValidation;
using Microsoft.Extensions.Localization;
using Shifa.Clinic.Models;
using Shifa.Core;

namespace Shifa.Clinic.Validators;

internal static class ClinicRules
{
    public static bool IsValidTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return true;
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}

public class AddBranchValidator : AbstractValidator<AddBranchModel>
{
    public AddBranchValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
        RuleFor(x => x.TimeZoneId).Must(ClinicRules.IsValidTimeZone).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Phone).MaximumLength(30).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Address).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}

public class UpdateBranchValidator : AbstractValidator<UpdateBranchModel>
{
    public UpdateBranchValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).WithMessage(_ => l["Required"]);
        RuleFor(x => x.TimeZoneId).Must(ClinicRules.IsValidTimeZone).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Phone).MaximumLength(30).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Address).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}

public class AddSpecialtyValidator : AbstractValidator<AddSpecialtyModel>
{
    public AddSpecialtyValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
    }
}

public class UpdateSpecialtyValidator : AbstractValidator<UpdateSpecialtyModel>
{
    public UpdateSpecialtyValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
    }
}

public class AddDoctorValidator : AbstractValidator<AddDoctorModel>
{
    public AddDoctorValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullNameEn).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Title).MaximumLength(100).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.SpecialtyIds).NotEmpty().WithMessage(_ => l["Required"]);
    }
}

public class UpdateDoctorValidator : AbstractValidator<UpdateDoctorModel>
{
    public UpdateDoctorValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.FullNameEn).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.Title).MaximumLength(100).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.SpecialtyIds).NotEmpty().WithMessage(_ => l["Required"]);
    }
}

public class SetDoctorScheduleValidator : AbstractValidator<SetDoctorScheduleModel>
{
    public SetDoctorScheduleValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Items).NotNull().WithMessage(_ => l["Required"]);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DayOfWeek).IsInEnum().WithMessage(_ => l["InvalidInput"]);
            item.RuleFor(i => i.SlotMinutes).InclusiveBetween(5, 240).WithMessage(_ => l["InvalidInput"]);
            item.RuleFor(i => i).Must(i => i.EndTime > i.StartTime).WithMessage(_ => l["InvalidSchedule"]);
            item.RuleFor(i => i).Must(i => i.SlotMinutes > 0 && (i.EndTime - i.StartTime).TotalMinutes % i.SlotMinutes == 0)
                .WithMessage(_ => l["InvalidSchedule"]);
        });
    }
}

public class AddTimeOffValidator : AbstractValidator<AddTimeOffModel>
{
    public AddTimeOffValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.ToAt).GreaterThan(x => x.FromAt).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage(_ => l["InvalidInput"]);
    }
}

public class AddServiceValidator : AbstractValidator<AddServiceModel>
{
    public AddServiceValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480).WithMessage(_ => l["InvalidInput"]);
    }
}

public class UpdateServiceValidator : AbstractValidator<UpdateServiceModel>
{
    public UpdateServiceValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200).WithMessage(_ => l["Required"]);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480).WithMessage(_ => l["InvalidInput"]);
    }
}

public class SetServicePriceValidator : AbstractValidator<SetServicePriceModel>
{
    public SetServicePriceValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, false).WithMessage(_ => l["InvalidInput"]);
    }
}
