using CarCareTracker.Helper;
using CarCareTracker.Models.Settings;
using Xunit;

namespace CarCareTracker.Tests.Helper;

public class LocaleHelperTests
{
    private readonly LocaleHelper _helper = new();

    [Fact]
    public void BuildRequestLocalizationOptions_UsesProvidedCulture_WhenValid()
    {
        var config = new ServerConfig { LocaleOverride = "en-CA" };

        var options = _helper.BuildRequestLocalizationOptions(config);

        Assert.Equal("en-CA", options.DefaultRequestCulture.Culture.Name);
        Assert.Single(options.SupportedCultures);
        Assert.Equal("en-CA", options.SupportedCultures[0].Name);
        Assert.Single(options.SupportedUICultures);
        Assert.Equal("en-CA", options.SupportedUICultures[0].Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void BuildRequestLocalizationOptions_FallsBackToEnUs_WhenNullOrEmpty(string? locale)
    {
        var config = new ServerConfig { LocaleOverride = locale };

        var options = _helper.BuildRequestLocalizationOptions(config);

        Assert.Equal("en-US", options.DefaultRequestCulture.Culture.Name);
        Assert.Single(options.SupportedCultures);
        Assert.Equal("en-US", options.SupportedCultures[0].Name);
        Assert.Single(options.SupportedUICultures);
        Assert.Equal("en-US", options.SupportedUICultures[0].Name);
    }

    [Fact]
    public void BuildRequestLocalizationOptions_FallsBackToEnUs_WhenInvalidCulture()
    {
        var config = new ServerConfig { LocaleOverride = "xx-INVALID" };

        var options = _helper.BuildRequestLocalizationOptions(config);

        Assert.Equal("en-US", options.DefaultRequestCulture.Culture.Name);
        Assert.Single(options.SupportedCultures);
        Assert.Equal("en-US", options.SupportedCultures[0].Name);
        Assert.Single(options.SupportedUICultures);
        Assert.Equal("en-US", options.SupportedUICultures[0].Name);
    }
}
