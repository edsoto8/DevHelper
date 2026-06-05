using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public interface ISystemRepository
{
    Task<IEnumerable<SystemEntry>> GetAllAsync();
    Task<IEnumerable<SystemEntry>> GetActiveAsync();
    Task<SystemEntry?> GetByIdAsync(int id);
    Task<int> CreateAsync(SystemEntry system);
    Task UpdateAsync(SystemEntry system);
    Task DeactivateAsync(int id);
}
