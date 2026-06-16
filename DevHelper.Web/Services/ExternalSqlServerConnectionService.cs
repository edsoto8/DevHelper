using Microsoft.Data.SqlClient;

namespace DevHelper.Web.Services;

public sealed record ConnectionTestResult(bool Success, string Message);

/// <summary>
/// Stores and tests the optional external SQL Server connection string. v1 performs
/// no ticket lookup. Absent or unreachable servers never block manual ticket entry.
/// </summary>
public interface IExternalSqlServerConnectionService
{
    Task<bool> HasConnectionStringAsync();
    Task<ConnectionTestResult> TestConnectionAsync(string? connectionStringOverride = null, CancellationToken cancellationToken = default);
}

public sealed class ExternalSqlServerConnectionService : IExternalSqlServerConnectionService
{
    private readonly IApplicationSettingsService _settings;
    private readonly ILogger<ExternalSqlServerConnectionService> _logger;

    public ExternalSqlServerConnectionService(
        IApplicationSettingsService settings,
        ILogger<ExternalSqlServerConnectionService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> HasConnectionStringAsync()
        => !string.IsNullOrWhiteSpace(await _settings.GetTicketLookupConnectionStringAsync());

    public async Task<ConnectionTestResult> TestConnectionAsync(
        string? connectionStringOverride = null, CancellationToken cancellationToken = default)
    {
        var connectionString = connectionStringOverride
            ?? await _settings.GetTicketLookupConnectionStringAsync();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogInformation("SQL Server connection test skipped: no connection string configured.");
            return new ConnectionTestResult(false, "No connection string configured.");
        }

        // Extract host for logging without leaking credentials.
        string host = "(unknown)";
        try
        {
            host = new SqlConnectionStringBuilder(connectionString).DataSource;
        }
        catch
        {
            // Ignore parse failures for the log-only host hint.
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            _logger.LogInformation("SQL Server connection test succeeded for host {Host}.", host);
            return new ConnectionTestResult(true, "Connection succeeded.");
        }
        catch (Exception ex)
        {
            // Log type/message and host only — never the connection string or credentials.
            _logger.LogWarning("SQL Server connection test failed for host {Host}: {ExceptionType} {Message}.",
                host, ex.GetType().Name, ex.Message);
            return new ConnectionTestResult(false, $"Connection failed: {ex.Message}");
        }
    }
}
