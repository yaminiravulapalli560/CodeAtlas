namespace CodeAtlas.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using CodeAtlas.Common.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class SystemEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SystemEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_Returns200Ok_AndHealthyStatus()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task SystemStatusEndpoint_ReturnsHealthy_WithAllExpectedModules()
    {
        var response = await _client.GetAsync("/api/v1/system/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();

        Assert.NotNull(status);
        Assert.Equal("Healthy", status.Status);
        Assert.Equal("CodeAtlas Web API", status.Service);
        Assert.Equal("0.1.0-alpha", status.Version);
        Assert.Contains("Identity", status.ActiveModules);
        Assert.Contains("Repositories", status.ActiveModules);
        Assert.Contains("CodeAnalysis", status.ActiveModules);
        Assert.Contains("Architecture", status.ActiveModules);
        Assert.Contains("AiAssistant", status.ActiveModules);
        Assert.Contains("Jobs", status.ActiveModules);
        Assert.Equal(6, status.ActiveModules.Count);
    }
}

