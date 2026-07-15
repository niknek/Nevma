namespace Nevma.ArchitectureTests;

public sealed class SolutionStructureTests
{
    [Fact]
    public void Services_do_not_reference_other_service_projects()
    {
        var root = FindRepositoryRoot();
        var serviceProjects = Directory.GetFiles(
            Path.Combine(root, "src", "Services"),
            "*.csproj",
            SearchOption.AllDirectories);

        Assert.NotEmpty(serviceProjects);

        var servicesRoot = Path.GetFullPath(Path.Combine(root, "src", "Services"));

        foreach (var project in serviceProjects)
        {
            foreach (var reference in ReadProjectReferences(project))
            {
                Assert.False(
                    reference.StartsWith(servicesRoot, StringComparison.OrdinalIgnoreCase),
                    $"Service project '{project}' must not reference another service project '{reference}'.");
            }
        }
    }

    [Fact]
    public void Deployable_projects_reference_service_defaults()
    {
        var root = FindRepositoryRoot();
        var deployableProjects = Directory.GetFiles(
                Path.Combine(root, "src", "Services"),
                "*.csproj",
                SearchOption.AllDirectories)
            .Append(Path.Combine(root, "src", "Gateway", "Nevma.Gateway", "Nevma.Gateway.csproj"));

        foreach (var project in deployableProjects)
        {
            var references = ReadProjectReferences(project);
            Assert.Contains(
                references,
                reference => reference.EndsWith(
                    Path.Combine("Nevma.ServiceDefaults", "Nevma.ServiceDefaults.csproj"),
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Application_layers_do_not_depend_on_infrastructure_namespaces()
    {
        var root = FindRepositoryRoot();
        var applicationFiles = Directory.GetFiles(
            Path.Combine(root, "src", "Services"),
            "*.cs",
            SearchOption.AllDirectories)
            .Where(path => path.Contains(
                $"{Path.DirectorySeparatorChar}Application{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase));

        foreach (var file in applicationFiles)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain(".Infrastructure", source, StringComparison.Ordinal);
        }
    }

    private static string[] ReadProjectReferences(string project)
    {
        var projectDirectory = Path.GetDirectoryName(project)
            ?? throw new DirectoryNotFoundException($"Could not locate project directory for '{project}'.");

        return System.Xml.Linq.XDocument.Load(project)
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar))
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)))
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Nevma.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
