using Microsoft.Extensions.Logging;
using SQLite;
using WeightRecall.Models;

namespace WeightRecall.Data;

/// <summary>
/// Provides access to the SQLite database and handles its initialization.
/// </summary>
public class DatabaseContext
{
    private bool _isInitialized;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<DatabaseContext> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseContext"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostics.</param>
    public DatabaseContext(ILogger<DatabaseContext> logger)
    {
        _logger = logger;
        string databasePath = Path.Combine(FileSystem.AppDataDirectory, "WeightRecall.db3");
        _logger.LogInformation("Initializing database at {Path}", databasePath);
        Connection = new SQLiteAsyncConnection(databasePath);
    }

    /// <summary>
    /// Ensures that the database and its tables are created and ready for use.
    /// This method is thread-safe and only performs initialization once.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    /// <exception cref="Exception">Thrown when database initialization fails.</exception>
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

    /// <summary>
    /// Gets the SQLite asynchronous connection.
    /// </summary>
    public SQLiteAsyncConnection Connection { get; }
}
