using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public interface IReleasePlanRepository
{
    Task<IEnumerable<ReleasePlan>> GetAllAsync();
    Task<ReleasePlan?> GetByIdAsync(int id);
    Task<int> CreateAsync(ReleasePlan plan);
    Task UpdateAsync(ReleasePlan plan);
    Task DeleteAsync(int id);
    Task SaveWithChildrenAsync(ReleasePlan plan);
}
