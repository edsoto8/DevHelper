using Microsoft.Data.SqlClient;
using Serilog;

namespace ReleasePlanGenerator.Web.Services;

public class ExternalSqlServerConnectionService : IExternalSqlServerConnectionService
{
    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            Log.Information("External SQL Server connection test succeeded.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "External SQL Server connection test failed.");
            return false;
        }
    }
}
