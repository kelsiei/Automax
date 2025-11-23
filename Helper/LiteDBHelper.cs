using LiteDB;
using System.IO;
using Microsoft.Extensions.Logging;

namespace CarCareTracker.Helper;

public class LiteDBHelper : IDisposable
{
    private readonly ILogger<LiteDBHelper> _logger;
    private readonly LiteDatabase _database;
    private bool _disposed;

    public LiteDBHelper(ILogger<LiteDBHelper> logger)
    {
        _logger = logger;

        StaticHelper.EnsureDataDirectoriesExist(logger);

        var dbPath = Path.Combine(StaticHelper.DataDirectory, "cartracker.db");
        var connectionString = $"Filename={dbPath};Connection=shared";

        _logger.LogInformation("Initializing LiteDB at {DatabasePath}", dbPath);

        _database = new LiteDatabase(connectionString);
    }

    public LiteDatabase Database
    {
        get
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LiteDBHelper));
            }
            return _database;
        }
    }

    public ILiteCollection<T> GetCollection<T>(string name)
    {
        return Database.GetCollection<T>(name);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _database.Dispose();
        _disposed = true;
        _logger.LogInformation("Disposed LiteDB database.");
    }
}
