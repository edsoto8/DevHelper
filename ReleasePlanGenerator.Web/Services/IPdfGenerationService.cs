namespace ReleasePlanGenerator.Web.Services;

public interface IPdfGenerationService
{
    Task<byte[]> GenerateFromMarkdownAsync(string markdown, string title);
}
