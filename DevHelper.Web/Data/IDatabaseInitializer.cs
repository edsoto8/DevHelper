namespace DevHelper.Web.Data;

/// <summary>
/// Ensures the SQLite database exists and applies pending migrations on startup.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Creates the database and SchemaMigrations table if missing, then applies every
    /// migration not yet recorded, in filename order. Idempotent across repeated calls.
    /// </summary>
    /// <returns>The names of migrations applied during this call.</returns>
    Task<IReadOnlyList<string>> InitializeAsync(CancellationToken cancellationToken = default);
}
