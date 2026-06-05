using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public interface IReleaseTemplateRepository
{
    Task<IEnumerable<ReleaseTemplate>> GetAllAsync();
    Task<IEnumerable<ReleaseTemplate>> GetActiveAsync();
    Task<ReleaseTemplate?> GetByIdAsync(int id);
    Task<ReleaseTemplate?> GetDefaultAsync();
    Task<int> CreateAsync(ReleaseTemplate template);
    Task UpdateAsync(ReleaseTemplate template);
    Task SetDefaultAsync(int id);
    Task DeleteAsync(int id);
}
