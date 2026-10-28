namespace CodeAtlas.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CodeAtlas.Modules.Repositories.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class RepositoryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RepositoryEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRepositories_ReturnsOk_AndList()
    {
        var response = await _client.GetAsync("/api/v1/repositories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var repos = await response.Content.ReadFromJsonAsync<List<Repository>>();
        Assert.NotNull(repos);
    }

    [Fact]
    public async Task CreateRepository_WithValidData_ReturnsCreated_AndCanBeRetrieved()
    {
        var uniqueName = $"repo-{Guid.NewGuid():N}";
        var request = new CreateRepositoryRequest(
            Name: uniqueName,
            Description: "A test repository for unit testing",
            SourceType: "Local",
            RepositoryUrl: null);

        var response = await _client.PostAsJsonAsync("/api/v1/repositories", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<Repository>();
        Assert.NotNull(created);
        Assert.Equal(uniqueName, created.Name);
        Assert.Equal("A test repository for unit testing", created.Description);
        Assert.Equal("Local", created.SourceType);
        Assert.Null(created.RepositoryUrl);
        Assert.NotEqual(Guid.Empty, created.Id);

        // Verify it can be retrieved by ID
        var getResponse = await _client.GetAsync($"/api/v1/repositories/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Repository>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(uniqueName, fetched.Name);
    }

    [Fact]
    public async Task CreateRepository_WithMissingName_ReturnsBadRequest()
    {
        var request = new CreateRepositoryRequest(
            Name: "",
            Description: "Valid description",
            SourceType: "Local",
            RepositoryUrl: null);

        var response = await _client.PostAsJsonAsync("/api/v1/repositories", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Name"));
    }

    [Fact]
    public async Task CreateRepository_WithDuplicateName_ReturnsConflict()
    {
        var uniqueName = $"dup-repo-{Guid.NewGuid():N}";
        var request1 = new CreateRepositoryRequest(
            Name: uniqueName,
            Description: "First repo",
            SourceType: "Local",
            RepositoryUrl: null);

        var response1 = await _client.PostAsJsonAsync("/api/v1/repositories", request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Attempt creating another with same name (different casing)
        var request2 = new CreateRepositoryRequest(
            Name: uniqueName.ToUpperInvariant(),
            Description: "Duplicate name repo",
            SourceType: "Remote",
            RepositoryUrl: "https://github.com/example/repo");

        var response2 = await _client.PostAsJsonAsync("/api/v1/repositories", request2);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

        var problem = await response2.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Name"));
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp://example.com/repo")]
    [InlineData("relative/path/repo")]
    public async Task CreateRepository_WithInvalidUrl_ReturnsBadRequest(string invalidUrl)
    {
        var uniqueName = $"invalid-url-{Guid.NewGuid():N}";
        var request = new CreateRepositoryRequest(
            Name: uniqueName,
            Description: "Invalid url repo",
            SourceType: "Remote",
            RepositoryUrl: invalidUrl);

        var response = await _client.PostAsJsonAsync("/api/v1/repositories", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("RepositoryUrl"));
    }

    [Fact]
    public async Task GetRepository_WhenNotFound_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/repositories/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

