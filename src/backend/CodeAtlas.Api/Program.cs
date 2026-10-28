using CodeAtlas.Common.Models;
using CodeAtlas.Common.Modules;
using CodeAtlas.Modules.AiAssistant;
using CodeAtlas.Modules.Architecture;
using CodeAtlas.Modules.CodeAnalysis;
using CodeAtlas.Modules.Identity;
using CodeAtlas.Modules.Jobs;
using CodeAtlas.Modules.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS for local development
const string FrontendCorsPolicy = "FrontendPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    // Default safe fallback if configuration is missing in dev
    allowedOrigins = ["http://localhost:5173", "http://127.0.0.1:5173"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure ASP.NET Core built-in Health Checks
builder.Services.AddHealthChecks();

// Explicitly register modular monolith modules (no reflection / assembly scanning)
var modules = new IModule[]
{
    new IdentityModule(),
    new RepositoriesModule(),
    new CodeAnalysisModule(),
    new ArchitectureModule(),
    new AiAssistantModule(),
    new JobsModule()
};

builder.Services.AddConfiguredModules(builder.Configuration, modules);

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Map Core System Endpoints
app.MapHealthChecks("/health");

app.MapGet("/api/v1/system/status", (IReadOnlyCollection<IModule> activeModules, IWebHostEnvironment env) =>
{
    var response = new SystemStatusResponse(
        Status: "Healthy",
        Service: "CodeAtlas Web API",
        Environment: env.EnvironmentName,
        Version: "0.1.0-alpha",
        TimestampUtc: DateTimeOffset.UtcNow,
        ActiveModules: activeModules.Select(m => m.Name).ToArray()
    );

    return Results.Ok(response);
});

// Map Module-specific endpoints
app.MapConfiguredModules(modules);

app.Run();

// Expose Program class for integration testing with WebApplicationFactory
public partial class Program { }
