using ReleasePlanGenerator.Web.Models;

namespace ReleasePlanGenerator.Web.Services;

public interface IMarkdownGenerationService
{
    Task<string> GenerateAsync(ReleasePlan plan);
}
