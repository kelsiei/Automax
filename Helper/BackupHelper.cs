using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace CarCareTracker.Helper;

public class BackupHelper
{
    private readonly ILogger<BackupHelper> _logger;

    public BackupHelper(ILogger<BackupHelper> logger)
    {
        _logger = logger;
    }

    public (byte[] Content, string FileName) CreateLiteDbBackupZip()
    {
        var dataDirectory = StaticHelper.DataDirectory;
        var fileName = $"carcare-backup-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";

        using var ms = new MemoryStream();

        try
        {
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                if (Directory.Exists(dataDirectory))
                {
                    AddDirectoryToArchive(archive, dataDirectory, dataDirectory);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup ZIP from data directory {DataDirectory}.", dataDirectory);
        }

        return (ms.ToArray(), fileName);
    }

    private void AddDirectoryToArchive(ZipArchive archive, string rootPath, string currentPath)
    {
        try
        {
            foreach (var filePath in Directory.GetFiles(currentPath))
            {
                try
                {
                    var relativePath = Path.GetRelativePath(rootPath, filePath)
                        .Replace(Path.DirectorySeparatorChar, '/');

                    var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var fileStream = File.OpenRead(filePath);
                    fileStream.CopyTo(entryStream);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add file {FilePath} to backup archive.", filePath);
                }
            }

            foreach (var directory in Directory.GetDirectories(currentPath))
            {
                AddDirectoryToArchive(archive, rootPath, directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to traverse directory {Directory}.", currentPath);
        }
    }
}

