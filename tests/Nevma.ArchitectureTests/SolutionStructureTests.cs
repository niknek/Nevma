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

        foreach (var project in serviceProjects)
        {
            var projectText = File.ReadAllText(project);
            Assert.DoesNotContain("..\\Identity\\", projectText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("..\\Planning\\", projectText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("..\\Messaging\\", projectText, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Nevma.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
