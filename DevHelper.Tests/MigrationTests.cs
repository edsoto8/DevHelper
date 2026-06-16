using Dapper;

namespace DevHelper.Tests;

public sealed class MigrationTests
{
    [Fact]
    public async Task Initialize_creates_database_and_applies_migrations_in_order()
    {
        await using var db = await TestDatabase.CreateAsync(applyMigrations: false);
        var initializer = db.CreateInitializer();

        var applied = await initializer.InitializeAsync();

        Assert.Equal(new[] { "001_InitialSchema.sql", "002_SeedData.sql" }, applied.ToArray());
        Assert.True(File.Exists(db.DatabasePath));
    }

    [Fact]
    public async Task Migrations_are_recorded_in_SchemaMigrations()
    {
        await using var db = await TestDatabase.CreateAsync();
        using var connection = db.ConnectionFactory.CreateConnection();

        var names = (await connection.QueryAsync<string>(
            "SELECT MigrationName FROM SchemaMigrations ORDER BY MigrationName;")).ToList();

        Assert.Contains("001_InitialSchema.sql", names);
        Assert.Contains("002_SeedData.sql", names);
    }

    [Fact]
    public async Task Initialize_is_idempotent_on_repeated_runs()
    {
        await using var db = await TestDatabase.CreateAsync();
        var initializer = db.CreateInitializer();

        var secondRun = await initializer.InitializeAsync();

        Assert.Empty(secondRun);

        using var connection = db.ConnectionFactory.CreateConnection();
        var migrationCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM SchemaMigrations;");
        Assert.Equal(2, migrationCount);
    }

    [Fact]
    public async Task Seed_data_creates_default_release_template_and_systems()
    {
        await using var db = await TestDatabase.CreateAsync();
        using var connection = db.ConnectionFactory.CreateConnection();

        var templateCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Templates WHERE ToolType = 'ReleasePlan' AND IsDefault = 1;");
        var systemCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM SystemEntries;");

        Assert.Equal(1, templateCount);
        Assert.True(systemCount >= 1);
    }

    [Fact]
    public async Task Schema_contains_expected_tables()
    {
        await using var db = await TestDatabase.CreateAsync();
        using var connection = db.ConnectionFactory.CreateConnection();

        var tables = (await connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table';")).ToHashSet();

        foreach (var expected in new[]
        {
            "SchemaMigrations", "SystemEntries", "ServerEntries", "DatabaseEntries", "Templates",
            "ReleasePlans", "ReleasePlanTickets", "SqlScripts", "ReleasePlanSystems",
            "ReleasePlanServers", "ReleasePlanDatabases", "TestPlans", "TestPlanTickets",
            "TestCases", "PlanScreenshots", "ApplicationSettings",
        })
        {
            Assert.Contains(expected, tables);
        }
    }
}
