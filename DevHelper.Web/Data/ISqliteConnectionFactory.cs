using System.Data;

namespace DevHelper.Web.Data;

/// <summary>
/// Creates open SQLite connections. One connection per repository method,
/// consumed with <c>using var connection = _factory.CreateConnection();</c>.
/// </summary>
public interface ISqliteConnectionFactory
{
    /// <summary>Creates and opens a new SQLite connection with foreign keys enabled.</summary>
    IDbConnection CreateConnection();

    /// <summary>The resolved absolute path to the SQLite database file.</summary>
    string DatabasePath { get; }
}
