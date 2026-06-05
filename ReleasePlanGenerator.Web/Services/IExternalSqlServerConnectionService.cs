namespace ReleasePlanGenerator.Web.Services;

public interface IExternalSqlServerConnectionService
{
    Task<bool> TestConnectionAsync(string connectionString);
}
