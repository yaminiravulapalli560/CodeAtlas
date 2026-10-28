
namespace CodeAtlas.Modules.CodeAnalysis;

using CodeAtlas.Common.Modules;
using CodeAtlas.Modules.CodeAnalysis.Models;
using CodeAtlas.Modules.CodeAnalysis.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class CodeAnalysisModule : IModule
{
    public string Name => "CodeAnalysis";

    public void RegisterModule(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IWorkspacePathValidator, WorkspacePathValidator>();
        services.AddScoped<IFolderInspectionService, FolderInspectionService>();
        services.AddScoped<ICSharpSourceAnalysisService, CSharpSourceAnalysisService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/code-analysis")
            .WithTags("CodeAnalysis");

        group.MapGet("/status", () => Results.Ok(new
        {
            Module = Name,
            Status = "Registered",
            Description = "C# parsing, symbol indexing, and static analysis."
        }));

        group.MapPost("/inspect", async (
            FolderInspectionRequest request,
            IFolderInspectionService service,
            CancellationToken ct) =>
        {
            var outcome = await service.InspectDirectoryAsync(request, ct);

            if (!outcome.Success)
            {
                return Results.Problem(
                    detail: outcome.ErrorMessage,
                    statusCode: outcome.StatusCode,
                    title: outcome.StatusCode switch
                    {
                        400 => "Invalid inspection request",
                        403 => "Access restricted",
                        404 => "Directory not found",
                        _ => "Inspection failed"
                    });
            }

            return Results.Ok(outcome.Result);
        });

        group.MapPost("/analyze", async (
            CSharpAnalysisRequest request,
            ICSharpSourceAnalysisService service,
            CancellationToken ct) =>
        {
            var outcome = await service.AnalyzeFileAsync(request, ct);

            if (!outcome.Success)
            {
                return Results.Problem(
                    detail: outcome.ErrorMessage,
                    statusCode: outcome.StatusCode,
                    title: outcome.StatusCode switch
                    {
                        400 => "Invalid C# analysis request",
                        403 => "Access restricted",
                        404 => "Source file not found",
                        413 => "Source file too large",
                        _ => "C# analysis failed"
                    });
            }

            return Results.Ok(outcome.Result);
        });
    }
}
