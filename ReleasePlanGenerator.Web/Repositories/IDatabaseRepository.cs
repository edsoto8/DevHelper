using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public interface IDatabaseRepository
{
    Task<IEnumerable<DatabaseEntry>> GetAllAsync();
    Task<IEnumerable<DatabaseEntry>> GetActiveAsync();
    Task<DatabaseEntry?> GetByIdAsync(int id);
    Task<int> CreateAsync(DatabaseEntry database);
    Task UpdateAsync(DatabaseEntry database);
    Task DeactivateAsync(int id);
}
