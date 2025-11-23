using System.Text;
using CarCareTracker.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CarCareTracker.Helper;

public class FileHelper
{
    private readonly ILogger<FileHelper> _logger;

    public FileHelper(ILogger<FileHelper> logger)
    {
        _logger = logger;
    }

    private static string GetDocumentsRoot()
    {
        return Path.Combine(StaticHelper.DataDirectory, "documents");
    }

    private static string GetVehicleDocumentsDirectory(int vehicleId)
    {
        return Path.Combine(GetDocumentsRoot(), vehicleId.ToString());
    }

    public Task<IList<string>> GetVehicleDocumentsAsync(int vehicleId)
    {
        var dir = GetVehicleDocumentsDirectory(vehicleId);
        if (!Directory.Exists(dir))
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        var files = Directory
            .EnumerateFiles(dir)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList()!;

        return Task.FromResult<IList<string>>(files);
    }

    public async Task<string?> SaveVehicleDocumentAsync(int vehicleId, IFormFile file, IEnumerable<string> allowedExtensions)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        var ext = Path.GetExtension(file.FileName) ?? string.Empty;
        if (!IsExtensionAllowed(ext, allowedExtensions))
        {
            _logger.LogWarning("Rejected file upload due to disallowed extension: {Extension}", ext);
            return null;
        }

        var dir = GetVehicleDocumentsDirectory(vehicleId);
        Directory.CreateDirectory(dir);

        var safeFileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, safeFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("Saved document {FileName} for vehicle {VehicleId}.", safeFileName, vehicleId);

        return safeFileName;
    }

    public Task<bool> DeleteVehicleDocumentAsync(int vehicleId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.FromResult(false);
        }

        var dir = GetVehicleDocumentsDirectory(vehicleId);
        var fullPath = Path.Combine(dir, fileName);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {FileName} for vehicle {VehicleId}.", fileName, vehicleId);
            return Task.FromResult(false);
        }
    }

    public FileStream? OpenVehicleDocumentStream(int vehicleId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var dir = GetVehicleDocumentsDirectory(vehicleId);
        var fullPath = Path.Combine(dir, fileName);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private static bool IsExtensionAllowed(string ext, IEnumerable<string> allowedExtensions)
    {
        if (string.IsNullOrEmpty(ext))
        {
            return false;
        }

        ext = ext.ToLowerInvariant();

        var normalizedAllowed = (allowedExtensions ?? Array.Empty<string>())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant());

        return normalizedAllowed.Contains(ext);
    }
}
