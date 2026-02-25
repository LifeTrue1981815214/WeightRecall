using Microsoft.Extensions.Logging;
using SQLite;
using WeightRecall.Models;

namespace WeightRecall.Data;

public class DatabaseContext
{
    private bool _isInitialized;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<DatabaseContext> _logger;

    public DatabaseContext(ILogger<DatabaseContext> logger)
    {
        _logger = logger;
        string databasePath = Path.Combine(FileSystem.AppDataDirectory, "WeightRecall.db3");
        _logger.LogInformation("Initializing database at {Path}", databasePath);
        Connection = new SQLiteAsyncConnection(databasePath);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            _logger.LogInformation("Creating database tables...");
            _ = await Connection.CreateTableAsync<RoutineItem>();
            _ = await Connection.CreateTableAsync<WorkoutLog>();

            _isInitialized = true;
            _logger.LogInformation("Database tables created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database.");
            throw;
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    public SQLiteAsyncConnection Connection { get; }
}
