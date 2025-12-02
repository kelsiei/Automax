using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Automax.Models.Document;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Automax.Migration;

internal class DocumentFilesMigrator
{
    private readonly string _liteDbPath;
    private readonly string _sourceRoot;
    private readonly string _targetRoot;
    private readonly ILogger<DocumentFilesMigrator> _logger;

    public DocumentFilesMigrator(string liteDbPath, string sourceRoot, string targetRoot, ILogger<DocumentFilesMigrator> logger)
    {
        _liteDbPath = liteDbPath;
        _sourceRoot = sourceRoot;
        _targetRoot = targetRoot;
        _logger = logger;
    }

    public Task<int> MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting document file migration. Source={Source}, Target={Target}, DryRun={DryRun}", _sourceRoot, _targetRoot, dryRun);

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        var collection = lite.GetCollection<DocumentMetadata>("documents");

        var processed = 0;
        foreach (var doc in collection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (doc.Id == 0)
            {
                _logger.LogWarning("Skipping document with Id 0 (FileName={FileName}, VehicleId={VehicleId}).", doc.FileName, doc.VehicleId);
                continue;
            }

            if (doc.VehicleId == 0)
            {
                _logger.LogWarning("Skipping document {DocId} because VehicleId is 0 (FileName={FileName}).", doc.Id, doc.FileName);
                continue;
            }

            if (string.IsNullOrWhiteSpace(doc.FilePath))
            {
                _logger.LogWarning("Skipping document {DocId} because FilePath is null/empty (FileName={FileName}, VehicleId={VehicleId}).", doc.Id, doc.FileName, doc.VehicleId);
                continue;
            }

            var relativePath = doc.FilePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var sourcePath = Path.Combine(_sourceRoot, relativePath);
            var targetPath = Path.Combine(_targetRoot, relativePath);

            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Source file not found for document {DocId}: {SourcePath}", doc.Id, sourcePath);
                continue;
            }

            if (File.Exists(targetPath))
            {
                _logger.LogInformation("Skipping copy for document {DocId}; target already exists at {TargetPath}.", doc.Id, targetPath);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would copy document {DocId} from {SourcePath} to {TargetPath}.", doc.Id, sourcePath, targetPath);
                processed++;
                continue;
            }

            try
            {
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrWhiteSpace(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(sourcePath, targetPath, overwrite: false);
                processed++;
                _logger.LogInformation("Copied document {DocId} to {TargetPath}.", doc.Id, targetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy document {DocId} from {SourcePath} to {TargetPath}.", doc.Id, sourcePath, targetPath);
            }
        }

        _logger.LogInformation("Document file migration finished. FilesProcessed={Processed}. DryRun={DryRun}", processed, dryRun);
        return Task.FromResult(processed);
    }
}
