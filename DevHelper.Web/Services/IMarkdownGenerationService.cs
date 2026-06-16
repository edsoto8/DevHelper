using DevHelper.Web.Models;

namespace DevHelper.Web.Services;

/// <summary>
/// Renders plan Markdown from a template using the contract in spec section 5.5.
/// Shared across tools; Phase 1 implements release plan rendering.
/// </summary>
public interface IMarkdownGenerationService
{
    /// <summary>Renders a release plan template against resolved plan data.</summary>
    string GenerateReleasePlanMarkdown(string template, ReleasePlanRenderModel model);
}
