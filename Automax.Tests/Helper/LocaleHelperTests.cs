#nullable enable
using Automax.Helper;
using Automax.Models.Settings;
using Xunit;

namespace Automax.Tests.Helper;

public class LocaleHelperTests
{
    private readonly LocaleHelper _helper = new();

    [Fact]
    public void BuildRequestLocalizationOptions_UsesProvidedCulture_WhenValid()
    {
        var config = new ServerConfig { LocaleOverride = "en-CA" };

        var options = _helper.BuildRequestLocalizationOptions(config);

        Assert.Equal("en-CA", options.DefaultRequestCulture.Culture.Name);
        Assert.NotNull(options.SupportedCultures);
        var supportedCultures = options.SupportedCultures!;
        Assert.Single(supportedCultures);
        Assert.Equal("en-CA", supportedCultures[0].Name);
        Assert.NotNull(options.SupportedUICultures);
        var supportedUiCultures = options.SupportedUICultures!;
        Assert.Single(supportedUiCultures);
        Assert.Equal("en-CA", supportedUiCultures[0].Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void BuildRequestLocalizationOptions_FallsBackToEnUs_WhenNullOrEmpty(string? locale)
    {
        var config = new ServerConfig { LocaleOverride = locale };

        var options = _helper.BuildRequestLocalizationOptions(config);

        Assert.Equal("en-US", options.DefaultRequestCulture.Culture.Name);
        Assert.NotNull(options.SupportedCultures);
        var supportedCultures = options.SupportedCultures!;
        Assert.Single(supportedCultures);
        Assert.Equal("en-US", supportedCultures[0].Name);
        Assert.NotNull(options.SupportedUICultures);
        var supportedUiCultures = options.SupportedUICultures!;
        Assert.Single(supportedUiCultures);
        Assert.Equal("en-US", supportedUiCultures[0].Name);
    }

    [Fact]
    public void BuildRequestLocalizationOptions_FallsBackToEnUs_WhenInvalidCulture()
    {
        var config = new ServerConfig { LocaleOverride = "xx-INVALID" };

        var options = _helper.BuildRequestLocalizationOptions(config);

        Assert.Equal("en-US", options.DefaultRequestCulture.Culture.Name);
        Assert.NotNull(options.SupportedCultures);
        var supportedCultures = options.SupportedCultures!;
        Assert.Single(supportedCultures);
        Assert.Equal("en-US", supportedCultures[0].Name);
        Assert.NotNull(options.SupportedUICultures);
        var supportedUiCultures = options.SupportedUICultures!;
        Assert.Single(supportedUiCultures);
        Assert.Equal("en-US", supportedUiCultures[0].Name);
    }
}
