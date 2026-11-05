using System.Xml.Linq;

namespace Shifa.ArchitectureTests;

/// <summary>A project in the repository and the Shifa projects it references.</summary>
internal sealed record ProjectInfo(string Name, string RelativePath, IReadOnlyList<string> References);

/// <summary>
/// Reads every .csproj in the repository once. Module projects are the ones under src/Modules/.
/// </summary>
internal static class SolutionProjects
{
    public const string Api = "Shifa.API";
    public const string Core = "Shifa.Core";
    public const string Infrastructure = "Shifa.Infrastructure";

    private static readonly Lazy<IReadOnlyList<ProjectInfo>> LoadedProjects = new(Load);

    public static IReadOnlyList<ProjectInfo> All => LoadedProjects.Value;

    public static ProjectInfo Get(string name) => All.Single(p => p.Name == name);

    public static IEnumerable<ProjectInfo> ModuleImplementations =>
        All.Where(p => IsUnderModules(p) && !p.Name.EndsWith(".Contracts", StringComparison.Ordinal));

    public static IEnumerable<ProjectInfo> ModuleContracts =>
        All.Where(p => IsUnderModules(p) && p.Name.EndsWith(".Contracts", StringComparison.Ordinal));

    public static bool IsModuleImplementation(string projectName) =>
        ModuleImplementations.Any(p => p.Name == projectName);

    public static bool IsTestProject(ProjectInfo project) =>
        project.RelativePath.StartsWith("tests/", StringComparison.Ordinal);

    private static bool IsUnderModules(ProjectInfo project) =>
        project.RelativePath.StartsWith("src/Modules/", StringComparison.Ordinal);

    private static IReadOnlyList<ProjectInfo> Load()
    {
        var root = FindRepositoryRoot();

        return Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(path => new ProjectInfo(
                Name: Path.GetFileNameWithoutExtension(path),
                RelativePath: Path.GetRelativePath(root, path).Replace('\\', '/'),
                References: ReadProjectReferences(path)))
            .ToList();
    }

    private static IReadOnlyList<string> ReadProjectReferences(string csprojPath) =>
        XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .ToList();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Shifa.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find Shifa.slnx above the test output folder.");
    }
}
