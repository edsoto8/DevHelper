using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Repositories;

public interface IServerRepository
{
    Task<IEnumerable<ServerEntry>> GetAllAsync();
    Task<IEnumerable<ServerEntry>> GetActiveAsync();
    Task<ServerEntry?> GetByIdAsync(int id);
    Task<int> CreateAsync(ServerEntry server);
    Task UpdateAsync(ServerEntry server);
    Task DeactivateAsync(int id);
}
