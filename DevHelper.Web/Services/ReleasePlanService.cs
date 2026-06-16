using DevHelper.Web.Models;
using DevHelper.Web.Repositories;

namespace DevHelper.Web.Services;

/// <inheritdoc />
public sealed class ReleasePlanService : IReleasePlanService
{
    private readonly IReleasePlanRepository _planRepository;
    private readonly ISystemRepository _systemRepository;
    private readonly IServerRepository _serverRepository;
    private readonly IDatabaseRepository _databaseRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IMarkdownGenerationService _markdown;
    private readonly IScreenshotService _screenshots;
    private readonly ILogger<ReleasePlanService> _logger;

    public ReleasePlanService(
        IReleasePlanRepository planRepository,
        ISystemRepository systemRepository,
        IServerRepository serverRepository,
        IDatabaseRepository databaseRepository,
        ITemplateRepository templateRepository,
        IMarkdownGenerationService markdown,
        IScreenshotService screenshots,
        ILogger<ReleasePlanService> logger)
    {
        _planRepository = planRepository;
        _systemRepository = systemRepository;
        _serverRepository = serverRepository;
        _databaseRepository = databaseRepository;
        _templateRepository = templateRepository;
        _markdown = markdown;
        _screenshots = screenshots;
        _logger = logger;
    }

    public ValidationResult Validate(ReleasePlanAggregate aggregate)
    {
        var result = new ValidationResult();
        var plan = aggregate.Plan;

        if (string.IsNullOrWhiteSpace(plan.ReleaseDate))
        {
            result.Add("Release date is required.");
        }

        if (string.IsNullOrWhiteSpace(plan.Environment))
        {
            result.Add("Environment is required.");
        }

        if (string.IsNullOrWhiteSpace(plan.CreatedBy))
        {
            result.Add("Created By is required.");
        }

        if (plan.TemplateId is null or 0)
        {
            result.Add("A template must be selected.");
        }

        if (aggregate.Tickets.Count == 0 || aggregate.Tickets.All(t => string.IsNullOrWhiteSpace(t.TicketNumber)))
        {
            result.Add("At least one ticket with a ticket number is required.");
        }

        if (aggregate.Systems.Count == 0
            || aggregate.Systems.All(s => s.SystemEntryId is null && string.IsNullOrWhiteSpace(s.OtherSystemName)))
        {
            result.Add("At least one affected system is required.");
        }

        return result;
    }

    public async Task<string> GenerateMarkdownAsync(ReleasePlanAggregate aggregate)
    {
        var template = aggregate.Plan.TemplateId is { } templateId and not 0
            ? await _templateRepository.GetByIdAsync(templateId)
            : null;

        if (template is null)
        {
            throw new InvalidOperationException("Selected template was not found.");
        }

        var model = await BuildRenderModelAsync(aggregate);
        try
        {
            var output = _markdown.GenerateReleasePlanMarkdown(template.MarkdownTemplate, model);
            _logger.LogInformation("Generated Markdown for release plan {Title}.", aggregate.Plan.Title);
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Markdown generation failed for release plan {Title}.", aggregate.Plan.Title);
            throw;
        }
    }

    public async Task<(bool Saved, long Id, ValidationResult Validation)> SaveAsync(ReleasePlanAggregate aggregate)
    {
        var validation = Validate(aggregate);
        if (!validation.IsValid)
        {
            return (false, aggregate.Plan.Id, validation);
        }

        aggregate.Plan.MarkdownOutput = await GenerateMarkdownAsync(aggregate);
        var id = await _planRepository.SaveAsync(aggregate);
        return (true, id, validation);
    }

    public Task<IReadOnlyList<ReleasePlanSummary>> SearchAsync(string? titleQuery, string? environment)
        => _planRepository.SearchAsync(titleQuery, environment);

    public Task<ReleasePlanAggregate?> GetAggregateAsync(long id) => _planRepository.GetAggregateAsync(id);

    public Task DeleteAsync(long id) => _planRepository.DeleteAsync(id);

    /// <summary>Resolves all foreign keys into display values for Markdown rendering.</summary>
    private async Task<ReleasePlanRenderModel> BuildRenderModelAsync(ReleasePlanAggregate aggregate)
    {
        var systems = (await _systemRepository.GetAllAsync(includeInactive: true)).ToDictionary(s => s.Id);
        var servers = (await _serverRepository.GetAllAsync(includeInactive: true)).ToDictionary(s => s.Id);
        var databases = (await _databaseRepository.GetAllAsync(includeInactive: true)).ToDictionary(d => d.Id);

        var model = new ReleasePlanRenderModel
        {
            Title = aggregate.Plan.Title,
            ReleaseDate = aggregate.Plan.ReleaseDate,
            Environment = aggregate.Plan.Environment,
            CreatedBy = aggregate.Plan.CreatedBy,
            BackupSteps = aggregate.Plan.BackupSteps,
            DeploymentSteps = aggregate.Plan.DeploymentSteps,
            ValidationSteps = aggregate.Plan.ValidationSteps,
            RollbackSteps = aggregate.Plan.RollbackSteps,
            Notes = aggregate.Plan.Notes,
        };

        model.Tickets = aggregate.Tickets.Select(t => new TicketRenderItem
        {
            Id = t.Id,
            TicketNumber = t.TicketNumber,
            TicketName = t.TicketName,
            Summary = t.TicketSummary,
            SortOrder = t.SortOrder,
        }).ToList();

        model.SystemNames = aggregate.Systems
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .Select(s => s.SystemEntryId is { } id && systems.TryGetValue(id, out var entry)
                ? entry.Name
                : s.OtherSystemName ?? string.Empty)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        model.Servers = aggregate.Servers
            .Where(s => servers.ContainsKey(s.ServerEntryId))
            .Select(s =>
            {
                var entry = servers[s.ServerEntryId];
                return new ServerRenderItem
                {
                    Id = s.Id,
                    SortOrder = s.SortOrder,
                    Name = entry.Name,
                    Environment = entry.Environment,
                    ServerType = entry.ServerType,
                };
            }).ToList();

        model.Databases = aggregate.Databases
            .Where(d => databases.ContainsKey(d.DatabaseEntryId))
            .Select(d =>
            {
                var entry = databases[d.DatabaseEntryId];
                return new DatabaseRenderItem
                {
                    Id = d.Id,
                    SortOrder = d.SortOrder,
                    Name = entry.Name,
                    SqlServerInstance = entry.SqlServerInstance,
                };
            }).ToList();

        model.SqlScripts = aggregate.SqlScripts.Select(s => new SqlScriptRenderItem
        {
            Id = s.Id,
            ExecutionOrder = s.ExecutionOrder,
            DatabaseName = s.DatabaseEntryId is { } dbId && databases.TryGetValue(dbId, out var db) ? db.Name : string.Empty,
            ScriptName = s.ScriptName,
            IsRequired = s.IsRequired,
            Description = s.ScriptDescription,
        }).ToList();

        var screenshotItems = new List<ScreenshotRenderItem>();
        foreach (var shot in aggregate.Screenshots)
        {
            var resolved = await _screenshots.ResolveExistingAsync(shot.FilePath);
            screenshotItems.Add(new ScreenshotRenderItem
            {
                Id = shot.Id,
                SortOrder = shot.SortOrder,
                Description = shot.Description,
                FilePath = shot.FilePath,
                FileExists = resolved is not null,
            });
        }

        model.Screenshots = screenshotItems;
        return model;
    }
}
