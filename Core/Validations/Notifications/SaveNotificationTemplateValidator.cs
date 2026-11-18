using System.Text.RegularExpressions;
using Core.Common;
using Core.Models.Notifications;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Core.Validations.Notifications;

public partial class SaveNotificationTemplateValidator : AbstractValidator<SaveNotificationTemplateModel>
{
    public SaveNotificationTemplateValidator(IStringLocalizer<LanguageResource> l)
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Channel).IsInEnum().WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Language).Must(lang => NotificationPlaceholders.Languages.Contains(lang)).WithMessage(_ => l["InvalidInput"]);
        RuleFor(x => x.Body!)
            .NotEmpty().MaximumLength(600).WithMessage(_ => l["InvalidInput"])
            .Must((model, body) => Placeholder().Matches(body).All(m => NotificationPlaceholders.For(model.Kind).Contains(m.Groups[1].Value)))
            .WithMessage(_ => l["UnknownPlaceholder"])
            .When(x => x.Body is not null);
    }

    [GeneratedRegex(@"\{([A-Za-z]+)\}")]
    private static partial Regex Placeholder();
}
