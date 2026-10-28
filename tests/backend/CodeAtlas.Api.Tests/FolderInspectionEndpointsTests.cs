namespace CodeAtlas.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using CodeAtlas.Modules.CodeAnalysis.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class FolderInspectionEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly string _testWorkspaceRoot;

    public FolderInspectionEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _testWorkspaceRoot = Path.Combine(Path.GetTempPath(), $"CodeAtlas_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspaceRoot);

        // Configure WebApplicationFactory to use our temp test workspace root
        var customizedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Workspace:RootPath", _testWorkspaceRoot);
        });

        _client = customizedFactory.CreateClient();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testWorkspaceRoot))
            {
                Directory.Delete(_testWorkspaceRoot, recursive: true);
            }
        }
        catch
        {
            // Suppress cleanup failures in temp directory
        }
    }

    [Fact]
    public async Task Inspect_EmptyDirectory_ReturnsZeroFiles_AndOk()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/code-analysis/inspect", new FolderInspectionRequest("."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FolderInspectionResult>();

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalFiles);
        Assert.Empty(result.RelativeFilePaths);
        Assert.Empty(result.ExtensionCounts);
        Assert.Empty(result.ProjectClassifications);
    }

    [Fact]
    public async Task Inspect_ValidDirectory_WithNestedFiles_ReturnsAccurateInventory()
    {
        // Setup nested test structure
        var subDir = Path.Combine(_testWorkspaceRoot, "src", "components");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_testWorkspaceRoot, "index.html"), "<html></html>");
        File.WriteAllText(Path.Combine(_testWorkspaceRoot, "styles.css"), "body {}");
        File.WriteAllText(Path.Combine(subDir, "App.tsx"), "export const App = () => null;");
        File.WriteAllText(Path.Combine(subDir, "helper.ts"), "export const x = 1;");

        var response = await _client.PostAsJsonAsync("/api/v1/code-analysis/inspect", new FolderInspectionRequest("."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FolderInspectionResult>();

        Assert.NotNull(result);
        Assert.Equal(4, result.TotalFiles);
        Assert.Contains("index.html", result.RelativeFilePaths);
        Assert.Contains("styles.css", result.RelativeFilePaths);
        Assert.Contains("src/components/App.tsx", result.RelativeFilePaths);
        Assert.Contains("src/components/helper.ts", result.RelativeFilePaths);

        Assert.Equal(1, result.ExtensionCounts[".html"]);
        Assert.Equal(1, result.ExtensionCounts[".css"]);
        Assert.Equal(1, result.ExtensionCounts[".tsx"]);
        Assert.Equal(1, result.ExtensionCounts[".ts"]);

        Assert.Contains("JavaScript / TypeScript", result.ProjectClassifications);
        Assert.Contains("HTML / CSS", result.ProjectClassifications);
        Assert.DoesNotContain(".NET", result.ProjectClassifications);
    }

    [Fact]
    public async Task Inspect_IgnoresCommonDependencyAndGeneratedFolders()
    {
        // Setup ignored folders
        var nodeModules = Path.Combine(_testWorkspaceRoot, "node_modules", "package");
        var gitDir = Path.Combine(_testWorkspaceRoot, ".git", "objects");
        var binDir = Path.Combine(_testWorkspaceRoot, "bin", "Debug");
        var objDir = Path.Combine(_testWorkspaceRoot, "obj");
        var distDir = Path.Combine(_testWorkspaceRoot, "dist");
        var validDir = Path.Combine(_testWorkspaceRoot, "src");

        Directory.CreateDirectory(nodeModules);
        Directory.CreateDirectory(gitDir);
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(objDir);
        Directory.CreateDirectory(distDir);
        Directory.CreateDirectory(validDir);

        File.WriteAllText(Path.Combine(nodeModules, "index.js"), "module.exports = {};");
        File.WriteAllText(Path.Combine(gitDir, "gitobj"), "blob");
        File.WriteAllText(Path.Combine(binDir, "app.dll"), "dll");
        File.WriteAllText(Path.Combine(objDir, "app.o"), "o");
        File.WriteAllText(Path.Combine(distDir, "bundle.js"), "bundle");
        File.WriteAllText(Path.Combine(validDir, "Program.cs"), "class Program {}");

        var response = await _client.PostAsJsonAsync("/api/v1/code-analysis/inspect", new FolderInspectionRequest("."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FolderInspectionResult>();

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalFiles);
        Assert.Single(result.RelativeFilePaths);
        Assert.Equal("src/Program.cs", result.RelativeFilePaths[0]);
        Assert.Equal(1, result.ExtensionCounts[".cs"]);
        Assert.Contains(".NET", result.ProjectClassifications);
    }

    [Fact]
    public async Task Inspect_NonExistentDirectory_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/code-analysis/inspect",
            new FolderInspectionRequest("non-existent-subfolder"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Inspect_PathTraversalOutsideWorkspaceRoot_ReturnsForbidden()
    {
        var traversalPath = Path.Combine("..", "SomeOtherDirectory");
        var response = await _client.PostAsJsonAsync(
            "/api/v1/code-analysis/inspect",
            new FolderInspectionRequest(traversalPath));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Inspect_AbsoluteExternalPathOutsideRoot_ReturnsForbidden()
    {
        var externalTemp = Path.GetTempPath(); // Outside _testWorkspaceRoot
        var response = await _client.PostAsJsonAsync(
            "/api/v1/code-analysis/inspect",
            new FolderInspectionRequest(externalTemp));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

