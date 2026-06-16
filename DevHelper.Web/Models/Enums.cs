namespace DevHelper.Web.Models;

/// <summary>The tool a template or plan belongs to. Stored as TEXT.</summary>
public enum ToolType
{
    ReleasePlan,
    TestPlan,
}

/// <summary>Deployment environment. Stored as TEXT.</summary>
public static class Environments
{
    public const string Development = "Development";
    public const string QA = "QA";
    public const string UAT = "UAT";
    public const string Production = "Production";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Development, QA, UAT, Production, Other,
    };
}

/// <summary>Server role. Stored as TEXT.</summary>
public static class ServerTypes
{
    public const string Web = "Web";
    public const string App = "App";
    public const string Database = "Database";
    public const string File = "File";
    public const string Service = "Service";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Web, App, Database, File, Service, Other,
    };
}

/// <summary>Test plan overall status. Stored as TEXT.</summary>
public static class TestPlanStatuses
{
    public const string Draft = "Draft";
    public const string InProgress = "In Progress";
    public const string Completed = "Completed";
    public const string Failed = "Failed";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Draft, InProgress, Completed, Failed,
    };
}

/// <summary>Individual test case status. Stored as TEXT.</summary>
public static class TestCaseStatuses
{
    public const string NotRun = "Not Run";
    public const string Pass = "Pass";
    public const string Fail = "Fail";
    public const string Blocked = "Blocked";
    public const string Skipped = "Skipped";

    public static readonly IReadOnlyList<string> All = new[]
    {
        NotRun, Pass, Fail, Blocked, Skipped,
    };
}

/// <summary>Discriminator values used by <see cref="PlanScreenshot"/> and exports.</summary>
public static class PlanTypes
{
    public const string ReleasePlan = "ReleasePlan";
    public const string TestPlan = "TestPlan";
}
