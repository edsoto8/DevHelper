using System.Data;

namespace ReleasePlanGenerator.Web.Data;

public interface ISqliteConnectionFactory
{
    IDbConnection CreateConnection();
}
