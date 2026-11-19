namespace Shifa.Tests.Architecture;

/// <summary>Project layout and dependency direction (ADR-004, ADR-009): API → Infrastructure → Core, tests → API.</summary>
public class ProjectReferenceTests
{
    [Fact]
    public void Solution_has_exactly_the_HRM_style_projects_at_the_root()
    {
        var projects = SolutionProjects.All.Select(p => p.RelativePath).OrderBy(p => p).ToList();

        Assert.Equal(
            ["Core/Core.csproj", "Infrastructure/Infrastructure.csproj", "Shifa.Tests/Shifa.Tests.csproj", "Shifa/Shifa.csproj"],
            projects);
    }

    [Fact]
    public void Core_does_not_reference_any_project()
    {
        Assert.Empty(SolutionProjects.Get(SolutionProjects.Core).References);
    }

    [Fact]
    public void Infrastructure_references_only_Core()
    {
        Assert.Equal([SolutionProjects.Core], SolutionProjects.Get(SolutionProjects.Infrastructure).References);
    }

    [Fact]
    public void Api_references_only_Infrastructure()
    {
        Assert.Equal([SolutionProjects.Infrastructure], SolutionProjects.Get(SolutionProjects.Api).References);
    }

    [Fact]
    public void Tests_reference_only_the_Api()
    {
        Assert.Equal([SolutionProjects.Api], SolutionProjects.Get(SolutionProjects.Tests).References);
    }

    /// <summary>Data access stays in Infrastructure. (ASP.NET types like IFormFile are allowed in Core, as in Trio HRM.)</summary>
    [Fact]
    public void Core_assembly_does_not_depend_on_EF_Core_or_the_outer_projects()
    {
        var referenced = typeof(Core.LanguageResource).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "").ToList();

        Assert.DoesNotContain(referenced, name => name is "Infrastructure" or "Shifa"
            || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
