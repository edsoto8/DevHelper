using DevHelper.Web;
using DevHelper.Web.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevHelper.Tests;

/// <summary>
/// Creates an isolated temporary SQLite database with the real migrations applied,
/// plus a connection factory pointing at it. Dispose to delete the temp files.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly string _directory;

    private TestDatabase(string directory, ISqliteConnectionFactory factory)
    {
        _directory = directory;
        ConnectionFactory = factory;
    }

    public ISqliteConnectionFactory ConnectionFactory { get; }

    public string DatabasePath => ConnectionFactory.DatabasePath;

    public static async Task<TestDatabase> CreateAsync(bool applyMigrations = true)
    {
        var directory = Path.Combine(Path.GetTempPath(), "devhelper-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var dbPath = Path.Combine(directory, "test.db");

        var options = Options.Create(new DevHelperOptions { DatabasePath = dbPath });
        var factory = new SqliteConnectionFactory(options, new FakeHostEnvironment(directory));

        if (applyMigrations)
        {
            var initializer = new DatabaseInitializer(factory, NullLogger<DatabaseInitializer>.Instance);
            await initializer.InitializeAsync();
        }

        return new TestDatabase(directory, factory);
    }

    public DatabaseInitializer CreateInitializer()
        => new(ConnectionFactory, NullLogger<DatabaseInitializer>.Instance);

    public ValueTask DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>Minimal IHostEnvironment for tests.</summary>
internal sealed class FakeHostEnvironment : IHostEnvironment
{
    public FakeHostEnvironment(string contentRootPath) => ContentRootPath = contentRootPath;

    public string ApplicationName { get; set; } = "DevHelper.Tests";
    public string EnvironmentName { get; set; } = "Test";
    public string ContentRootPath { get; set; }
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
}

/// <summary>Shared logging/data-protection helpers for tests.</summary>
internal static class TestFactories
{
    public static ILogger<T> Logger<T>() => NullLogger<T>.Instance;

    public static IDataProtectionProvider DataProtection()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }
}
