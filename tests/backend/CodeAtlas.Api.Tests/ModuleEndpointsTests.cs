namespace CodeAtlas.Api.Tests;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class ModuleEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ModuleEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/v1/identity/status", "Identity")]
    [InlineData("/api/v1/repositories/status", "Repositories")]
    [InlineData("/api/v1/code-analysis/status", "CodeAnalysis")]
    [InlineData("/api/v1/architecture/status", "Architecture")]
    [InlineData("/api/v1/ai-assistant/status", "AiAssistant")]
    [InlineData("/api/v1/jobs/status", "Jobs")]
    public async Task ModuleStatusEndpoint_Returns200Ok_AndCorrectModuleName(string endpoint, string expectedModuleName)
    {
        var response = await _client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var moduleElement = doc.RootElement.GetProperty("module").GetString();
        var statusElement = doc.RootElement.GetProperty("status").GetString();

        Assert.Equal(expectedModuleName, moduleElement);
        Assert.Equal("Registered", statusElement);
    }
}

