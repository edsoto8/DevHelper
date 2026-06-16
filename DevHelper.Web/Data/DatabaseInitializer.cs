using System.Data;
using Dapper;

namespace DevHelper.Web.Data;

/// <inheritdoc />
public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private const string MigrationResourcePrefix = "DevHelper.Web.Database.Migrations.";

    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(ISqliteConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                """
                CREATE TABLE IF NOT EXISTS SchemaMigrations (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MigrationName TEXT NOT NULL UNIQUE,
                    AppliedAt TEXT NOT NULL
                );
                """);

            var applied = (await connection.QueryAsync<string>(
                "SELECT MigrationName FROM SchemaMigrations;")).ToHashSet(StringComparer.Ordinal);

            var appliedNow = new List<string>();

            foreach (var (name, sql) in GetMigrationScripts())
            {
                if (applied.Contains(name))
                {
                    continue;
                }

                using var transaction = connection.BeginTransaction();
                try
                {
                    await connection.ExecuteAsync(sql, transaction: transaction);
                    await connection.ExecuteAsync(
                        "INSERT INTO SchemaMigrations (MigrationName, AppliedAt) VALUES (@Name, @AppliedAt);",
                        new { Name = name, AppliedAt = DateTime.UtcNow.ToString("O") },
                        transaction);
                    transaction.Commit();
                    appliedNow.Add(name);
                    _logger.LogInformation("Applied database migration {MigrationName}.", name);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Failed to apply database migration {MigrationName}.", name);
                    throw;
                }
            }

            if (appliedNow.Count == 0)
            {
                _logger.LogInformation("Database is up to date; no migrations applied.");
            }

            return appliedNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }

    /// <summary>
    /// Returns embedded migration scripts ordered by their filename prefix.
    /// </summary>
    private static IEnumerable<(string Name, string Sql)> GetMigrationScripts()
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(MigrationResourcePrefix, StringComparison.Ordinal)
                        && n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal);

        foreach (var resourceName in resourceNames)
        {
            var migrationName = resourceName[MigrationResourcePrefix.Length..];
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Migration resource '{resourceName}' could not be opened.");
            using var reader = new StreamReader(stream);
            yield return (migrationName, reader.ReadToEnd());
        }
    }
}
