using DevHelper.Web.Models;
using DevHelper.Web.Repositories;

namespace DevHelper.Web.Services;

public sealed class ValidationResult
{
    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;

    public void Add(string error) => Errors.Add(error);
}

public interface IReleasePlanService
{
    ValidationResult Validate(ReleasePlanAggregate aggregate);

    /// <summary>Builds the resolved render model and generates Markdown without saving.</summary>
    Task<string> GenerateMarkdownAsync(ReleasePlanAggregate aggregate);

    /// <summary>Validates, generates+stores Markdown, and saves the plan transactionally.</summary>
    Task<(bool Saved, long Id, ValidationResult Validation)> SaveAsync(ReleasePlanAggregate aggregate);

    Task<IReadOnlyList<ReleasePlanSummary>> SearchAsync(string? titleQuery, string? environment);
    Task<ReleasePlanAggregate?> GetAggregateAsync(long id);
    Task DeleteAsync(long id);
}
