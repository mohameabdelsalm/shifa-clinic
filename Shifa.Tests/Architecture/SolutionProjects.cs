using System.Xml.Linq;

namespace Shifa.Tests.Architecture;

/// <summary>A project in the repository and the projects it references.</summary>
internal sealed record ProjectInfo(string Name, string RelativePath, IReadOnlyList<string> References);

/// <summary>
/// Reads every .csproj in the repository once. The layout follows Trio HRM (ADR-009): Core, Infrastructure and the API
/// (Shifa) at the root, plus Shifa.Tests. No src/ or tests/ folders, no per-module projects.
/// </summary>
internal static class SolutionProjects
{
    public const string Core = "Core";
    public const string Infrastructure = "Infrastructure";
    public const string Api = "Shifa";
    public const string Tests = "Shifa.Tests";

    private static readonly Lazy<IReadOnlyList<ProjectInfo>> LoadedProjects = new(Load);

    public static IReadOnlyList<ProjectInfo> All => LoadedProjects.Value;

    public static ProjectInfo Get(string name) => All.Single(p => p.Name == name);

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
