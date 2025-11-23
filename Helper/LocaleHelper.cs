using System.Globalization;
using CarCareTracker.Models.Settings;
using Microsoft.AspNetCore.Localization;

namespace CarCareTracker.Helper;

public class LocaleHelper
{
    public RequestLocalizationOptions BuildRequestLocalizationOptions(ServerConfig serverConfig)
    {
        var cultureName = string.IsNullOrWhiteSpace(serverConfig.LocaleOverride)
            ? "en-US"
            : serverConfig.LocaleOverride.Trim();

        CultureInfo culture;
        try
        {
            culture = new CultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            culture = new CultureInfo("en-US");
        }

        var options = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(culture),
            SupportedCultures = new List<CultureInfo> { culture },
            SupportedUICultures = new List<CultureInfo> { culture }
        };

        return options;
    }
}

