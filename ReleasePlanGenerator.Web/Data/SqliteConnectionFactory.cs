using System.Data;
using Microsoft.Data.Sqlite;

namespace ReleasePlanGenerator.Web.Data;

public class SqliteConnectionFactory(IConfiguration configuration) : ISqliteConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("Sqlite") ?? "Data Source=release-plan-generator.db";

    public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
}
