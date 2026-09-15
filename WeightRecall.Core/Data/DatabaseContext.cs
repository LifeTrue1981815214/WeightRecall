using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SQLite;
using WeightRecall.Models;

namespace WeightRecall.Data;

/// <summary>
/// Provides access to the SQLite database and handles its initialization.
/// </summary>
public class DatabaseContext : IAsyncDisposable
{
    private bool _isInitialized;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<DatabaseContext> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseContext"/> class.
    /// </summary>
    /// <param name="databasePath">The full path to the SQLite database file.</param>
    /// <param name="logger">The logger instance for diagnostics.</param>
    public DatabaseContext(string databasePath, ILogger<DatabaseContext> logger)
    {
        _logger = logger;
        _logger.LogInformation("Initializing database at {Path}", databasePath);
        Connection = new SQLiteAsyncConnection(databasePath);
    }

    public DatabaseContext(string tempDbPath)
    {
        _logger = NullLogger<DatabaseContext>.Instance;
        Connection = new SQLiteAsyncConnection(tempDbPath);
    }

    /// <summary>
    /// Creates an initialized, throwaway database for a single test.
    /// </summary>
    /// <remarks>
    /// Each call gets its own uniquely named file, which <see cref="DisposeAsync"/> deletes.
    /// A shared ":memory:" database would be simpler but is not safe here: sqlite-net pools
    /// connections by database path, so every caller of this method would be handed the same
    /// database. xUnit runs separate test classes in parallel, so tests would see each other's
    /// rows and one test's dispose would drop the tables out from under another.
    /// </remarks>
    /// <returns>A ready-to-use <see cref="DatabaseContext"/> isolated from every other test.</returns>
    public static async Task<DatabaseContext> CreateForTestingAsync()
    {
        string path = Path.Combine(Path.GetTempPath(), $"WeightRecall_test_{Guid.NewGuid():N}.db3");

        DatabaseContext context = new(path) { _databaseFileToDeleteOnDispose = path };
        await context.InitializeAsync();
        return context;
    }

    private string? _databaseFileToDeleteOnDispose;

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
            _ = await Connection.CreateTableAsync<PlannedExercise>();
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

    public async ValueTask DisposeAsync()
    {
        if (Connection != null)
            await Connection.CloseAsync();
        _semaphore.Dispose();

        if (_databaseFileToDeleteOnDispose is not null)
        {
            try
            {
                File.Delete(_databaseFileToDeleteOnDispose);
            }
            catch (IOException)
            {
                // A leftover temp file is not worth failing a test over.
            }
        }
    }
}
