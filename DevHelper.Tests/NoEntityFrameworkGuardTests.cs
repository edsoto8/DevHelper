using System.Reflection;
using DevHelper.Web.Services;

namespace DevHelper.Tests;

/// <summary>
/// Guards the spec rule: Entity Framework must not be used anywhere. Checks both the
/// compiled assembly's referenced assemblies and the web project source tree.
/// </summary>
public sealed class NoEntityFrameworkGuardTests
{
    [Fact]
    public void Web_assembly_does_not_reference_entity_framework()
    {
        var webAssembly = typeof(ReleasePlanService).Assembly;
        var referenced = webAssembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(referenced, name =>
            name.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
            || name.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Web_source_does_not_use_entity_framework_namespace()
    {
        var webProjectDir = LocateWebProjectDirectory();
        var offending = new List<string>();

        foreach (var file in Directory.EnumerateFiles(webProjectDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (text.Contains("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            {
                offending.Add(file);
            }
        }

        // Also check the project file for EF package references.
        var csproj = Path.Combine(webProjectDir, "DevHelper.Web.csproj");
        var csprojText = File.ReadAllText(csproj);
        Assert.DoesNotContain("EntityFrameworkCore", csprojText);

        Assert.True(offending.Count == 0,
            "Entity Framework namespace found in: " + string.Join(", ", offending));
    }

    private static string LocateWebProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "DevHelper.Web");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "DevHelper.Web.csproj")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the DevHelper.Web project directory.");
    }
}
