using System.Reflection;
using Dapper;
using Serilog;

namespace ReleasePlanGenerator.Web.Data;

public class DatabaseInitializer(ISqliteConnectionFactory connectionFactory) : IDatabaseInitializer
{
    public async Task InitializeAsync()
    {
        Log.Information("Initializing SQLite database.");

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS SchemaMigrations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MigrationName TEXT NOT NULL,
                AppliedAt TEXT NOT NULL
            );
            """);

        var applied = (await connection.QueryAsync<string>(
            "SELECT MigrationName FROM SchemaMigrations ORDER BY MigrationName")).ToHashSet();

        var migrations = GetOrderedMigrations();

        foreach (var (name, sql) in migrations)
        {
            if (applied.Contains(name))
                continue;

            using var tx = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(sql, transaction: tx);
                await connection.ExecuteAsync(
                    "INSERT INTO SchemaMigrations (MigrationName, AppliedAt) VALUES (@Name, @AppliedAt)",
                    new { Name = name, AppliedAt = DateTime.UtcNow.ToString("o") },
                    tx);
                tx.Commit();
                Log.Information("SQLite migration applied. MigrationName: {MigrationName}", name);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Log.Error(ex, "SQLite migration failed. MigrationName: {MigrationName}", name);
                throw;
            }
        }

        Log.Information("SQLite database initialization complete.");
    }

    private static List<(string Name, string Sql)> GetOrderedMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = "ReleasePlanGenerator.Web.Database.Migrations.";

        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix) && n.EndsWith(".sql"))
            .OrderBy(n => n)
            .Select(resourceName =>
            {
                using var stream = assembly.GetManifestResourceStream(resourceName)!;
                using var reader = new StreamReader(stream);
                var sql = reader.ReadToEnd();
                var name = resourceName[prefix.Length..].Replace(".sql", "");
                return (name, sql);
            })
            .ToList();
    }
}
