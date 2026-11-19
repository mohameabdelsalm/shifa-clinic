using Core.Interfaces.Reporting;
using Infrastructure.Repositories.Reporting;
using Infrastructure.Services.Reporting;
using QuestPDF.Infrastructure;

namespace Shifa.Extensions;

public static class ReportServiceExtensions
{
    /// <summary>
    /// Reports and the QuestPDF license (ADR-016). Reports:QuestPdfLicense = Community | Professional | Enterprise.
    /// Community is only for organizations under USD 1M annual gross revenue; the product owner sets the right one.
    /// Development falls back to Community; any other environment refuses to start without the setting.
    /// </summary>
    public static IServiceCollection AddShifaReports(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var license = configuration["Reports:QuestPdfLicense"];
        if (string.IsNullOrWhiteSpace(license))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Reports:QuestPdfLicense is not configured (Community, Professional or Enterprise).");
            license = nameof(LicenseType.Community);
        }

        QuestPDF.Settings.License = Enum.Parse<LicenseType>(license, ignoreCase: true);

        // Fonts (QuestPDF 2026.9+ uses only registered fonts and fails rather than substituting): font files deployed with
        // the app are found automatically (e.g. Shifa/Fonts/*.ttf, recommended for production, an Arabic font is needed);
        // Reports:UseSystemFonts also allows the machine's fonts (on Windows Arial/Tahoma cover Arabic).
        QuestPDF.Settings.UseSystemFonts = configuration.GetValue("Reports:UseSystemFonts", true);
        if (configuration["Reports:PdfFontFamilies"] is { Length: > 0 } fonts)
            PdfReportWriter.FontFamilies = fonts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        services.AddScoped<IReportRepo, ReportRepo>();
        return services;
    }
}
