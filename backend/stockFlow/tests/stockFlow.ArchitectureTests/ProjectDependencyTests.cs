using System.Xml.Linq;
using Xunit;

namespace stockFlow.ArchitectureTests;

public sealed class ProjectDependencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Domain_has_no_project_or_package_references()
    {
        var project = LoadProject("backend/stockFlow/src/stockFlow.Domain/stockFlow.Domain.csproj");

        Assert.Empty(ProjectReferences(project));
        Assert.Empty(PackageReferences(project));
    }

    [Fact]
    public void Domain_has_no_forbidden_framework_package_references()
    {
        var project = LoadProject("backend/stockFlow/src/stockFlow.Domain/stockFlow.Domain.csproj");
        var forbidden = new[] { "EntityFrameworkCore", "Identity", "AspNetCore", "Microsoft.AspNetCore" };

        Assert.DoesNotContain(PackageReferences(project), package =>
            forbidden.Any(term => package.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Application_references_only_domain()
    {
        var project = LoadProject("backend/stockFlow/src/stockFlow.Application/stockFlow.Application.csproj");

        Assert.Equal(
            ["stockFlow.Domain"],
            ProjectReferences(project).Select(Path.GetFileNameWithoutExtension).OrderBy(value => value));
        Assert.DoesNotContain(ProjectReferences(project), reference =>
            reference.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ProjectReferences(project), reference =>
            reference.Contains("API", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Infrastructure_references_application_and_domain()
    {
        var project = LoadProject("backend/stockFlow/src/stockFlow.Infrastructure/stockFlow.Infrastructure.csproj");

        Assert.Equal(
            ["stockFlow.Application", "stockFlow.Domain"],
            ProjectReferences(project).Select(Path.GetFileNameWithoutExtension).OrderBy(value => value));
    }

    [Fact]
    public void Api_references_application_and_infrastructure()
    {
        var project = LoadProject("backend/stockFlow/stockFlow.csproj");

        Assert.Equal(
            ["stockFlow.Application", "stockFlow.Infrastructure"],
            ProjectReferences(project).Select(Path.GetFileNameWithoutExtension).OrderBy(value => value));
    }

    [Fact]
    public void Required_projects_exist_in_the_solution_area()
    {
        var required = new[]
        {
            "backend/stockFlow/stockFlow.csproj",
            "backend/stockFlow/src/stockFlow.Domain/stockFlow.Domain.csproj",
            "backend/stockFlow/src/stockFlow.Application/stockFlow.Application.csproj",
            "backend/stockFlow/src/stockFlow.Infrastructure/stockFlow.Infrastructure.csproj",
            "backend/stockFlow/tests/stockFlow.UnitTests/stockFlow.UnitTests.csproj",
            "backend/stockFlow/tests/stockFlow.IntegrationTests/stockFlow.IntegrationTests.csproj",
            "backend/stockFlow/tests/stockFlow.ArchitectureTests/stockFlow.ArchitectureTests.csproj"
        };

        Assert.All(required, relativePath => Assert.True(File.Exists(Path.Combine(RepositoryRoot, relativePath)), relativePath));
    }

    private static XDocument LoadProject(string relativePath)
    {
        return XDocument.Load(Path.Combine(RepositoryRoot, relativePath));
    }

    private static IEnumerable<string> ProjectReferences(XDocument project)
    {
        return project.Descendants().Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty);
    }

    private static IEnumerable<string> PackageReferences(XDocument project)
    {
        return project.Descendants().Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")) &&
                File.Exists(Path.Combine(directory.FullName, "backend", "stockFlow", "stockFlow.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not locate the StockFlow repository root.");
    }
}
