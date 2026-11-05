namespace Shifa.ArchitectureTests;

/// <summary>
/// The project reference rules from ADR 0001 and CLAUDE.md.
/// Each test collects the violations and expects none, so a failure lists exactly what to fix.
/// </summary>
public class ProjectReferenceTests
{
    [Fact]
    public void Solution_has_at_least_one_module()
    {
        // Guards the other tests: if module discovery breaks, they would pass without checking anything.
        Assert.NotEmpty(SolutionProjects.ModuleImplementations);
    }

    [Fact]
    public void Every_module_has_a_Contracts_project()
    {
        var contractNames = SolutionProjects.ModuleContracts.Select(c => c.Name).ToHashSet();

        var missing = SolutionProjects.ModuleImplementations
            .Where(module => !contractNames.Contains($"{module.Name}.Contracts"))
            .Select(module => $"{module.Name} has no {module.Name}.Contracts project")
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void Core_does_not_reference_any_Shifa_project()
    {
        var core = SolutionProjects.Get(SolutionProjects.Core);

        Assert.Empty(core.References);
    }

    [Fact]
    public void Infrastructure_references_only_Core()
    {
        var violations = SolutionProjects.Get(SolutionProjects.Infrastructure).References
            .Where(reference => reference != SolutionProjects.Core)
            .Select(reference => $"{SolutionProjects.Infrastructure} -> {reference}")
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Contracts_reference_only_Core()
    {
        var violations = SolutionProjects.ModuleContracts
            .SelectMany(contracts => contracts.References
                .Where(reference => reference != SolutionProjects.Core)
                .Select(reference => $"{contracts.Name} -> {reference}"))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Modules_reference_other_modules_only_through_Contracts()
    {
        var violations = SolutionProjects.ModuleImplementations
            .SelectMany(module => module.References
                .Where(SolutionProjects.IsModuleImplementation)
                .Select(reference => $"{module.Name} -> {reference} (reference {reference}.Contracts instead)"))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Only_the_API_references_module_implementations()
    {
        var violations = SolutionProjects.All
            .Where(project => project.Name != SolutionProjects.Api && !SolutionProjects.IsTestProject(project))
            .SelectMany(project => project.References
                .Where(SolutionProjects.IsModuleImplementation)
                .Select(reference => $"{project.Name} -> {reference}"))
            .ToList();

        Assert.Empty(violations);
    }
}
