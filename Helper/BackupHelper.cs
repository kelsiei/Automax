using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Automax.Helper;

    public class BackupHelper
    {
        private readonly ILogger<BackupHelper> _logger;

        public BackupHelper(ILogger<BackupHelper> logger)
        {
            _logger = logger;
        }

        public virtual (byte[] Content, string FileName) CreateLiteDbBackupZip()
        {
        var dataDirectory = StaticHelper.DataDirectory;
        var fileName = $"automax-backup-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";

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

    public virtual Task<bool> RestoreLiteDbBackupAsync(Stream backupStream)
    {
        if (backupStream == null || !backupStream.CanRead || backupStream.Length == 0)
        {
            _logger.LogWarning("Restore aborted: provided backup stream is invalid or empty.");
            return Task.FromResult(false);
        }

        StaticHelper.EnsureDataDirectoriesExist(_logger);

        var dataDirectory = StaticHelper.DataDirectory;
        var tempRestoreDir = Path.Combine(StaticHelper.TempDirectory, $"restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRestoreDir);

        try
        {
            using (var archive = new ZipArchive(backupStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = Path.Combine(tempRestoreDir, entry.FullName);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            var backupExistingDir = $"{dataDirectory}_prev_{DateTime.UtcNow:yyyyMMddHHmmss}";
            if (Directory.Exists(dataDirectory))
            {
                Directory.Move(dataDirectory, backupExistingDir);
            }

            Directory.Move(tempRestoreDir, dataDirectory);
            _logger.LogInformation("Restore completed. Previous data folder moved to {PreviousDir}.", backupExistingDir);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup archive.");
            return Task.FromResult(false);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRestoreDir))
                {
                    Directory.Delete(tempRestoreDir, recursive: true);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up temporary restore directory {TempDir}.", tempRestoreDir);
            }
        }
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

