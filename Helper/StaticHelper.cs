using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Automax.Helper;

public static class StaticHelper
{
    public static readonly string VersionNumber = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.1.0";

    public static readonly string DataDirectory = "data";
    public static readonly string ConfigDirectory = Path.Combine(DataDirectory, "config");
    public static readonly string ImagesDirectory = Path.Combine(DataDirectory, "images");
    public static readonly string DocumentsDirectory = Path.Combine(DataDirectory, "documents");
    public static readonly string TranslationsDirectory = Path.Combine(DataDirectory, "translations");
    public static readonly string TempDirectory = Path.Combine(DataDirectory, "temp");
    public static readonly string WidgetsPath = Path.Combine(DataDirectory, "widgets.html");

    public static string SponsorsPath => Path.Combine(DataDirectory, "sponsors.json");
    public static string TranslationDirectoryPath => TranslationsDirectory;

    public static void EnsureDataDirectoriesExist(ILogger? logger = null)
    {
        string[] paths =
        {
            DataDirectory,
            ConfigDirectory,
            ImagesDirectory,
            DocumentsDirectory,
            TranslationsDirectory,
            TempDirectory
        };

        foreach (var path in paths)
        {
            Directory.CreateDirectory(path);
            logger?.LogInformation("Ensured directory: {Path}", path);
        }
    }

    public static string GetIconByFileExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "file-generic";
        }

        var normalized = extension.StartsWith(".") ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        return normalized switch
        {
            ".pdf" => "file-pdf",
            ".jpg" => "file-image",
            ".jpeg" => "file-image",
            ".png" => "file-image",
            ".gif" => "file-image",
            ".doc" => "file-doc",
            ".docx" => "file-doc",
            ".txt" => "file-text",
            _ => "file-generic" // TODO: expand mapping to fully match original application.
        };
    }
}
