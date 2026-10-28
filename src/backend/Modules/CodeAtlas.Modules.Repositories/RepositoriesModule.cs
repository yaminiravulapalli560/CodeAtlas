namespace CodeAtlas.Modules.Repositories;

using CodeAtlas.Common.Modules;
using CodeAtlas.Modules.Repositories.Models;
using CodeAtlas.Modules.Repositories.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class RepositoriesModule : IModule
{
    public string Name => "Repositories";

    public void RegisterModule(IServiceCollection services, IConfiguration configuration)
    {
        // In-memory repository store singleton (swappable with database implementation later)
        services.AddSingleton<IRepositoryStore, InMemoryRepositoryStore>();
        services.AddScoped<IRepositoryService, RepositoryService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/repositories")
            .WithTags("Repositories");

        group.MapGet("/status", () => Results.Ok(new
        {
            Module = Name,
            Status = "Registered",
            Description = "Git repository management and workspace catalog module boundary."
        }));

        group.MapGet("/", async (IRepositoryService service, CancellationToken ct) =>
        {
            var repositories = await service.GetAllAsync(ct);
            return Results.Ok(repositories);
        });

        group.MapGet("/{id:guid}", async (Guid id, IRepositoryService service, CancellationToken ct) =>
        {
            var repository = await service.GetByIdAsync(id, ct);
            return repository is not null
                ? Results.Ok(repository)
                : Results.NotFound(new { Message = $"Repository with ID '{id}' was not found." });
        });

        group.MapPost("/", async (CreateRepositoryRequest request, IRepositoryService service, CancellationToken ct) =>
        {
            var (repository, validation, isConflict) = await service.CreateAsync(request, ct);

            if (isConflict)
            {
                return Results.Conflict(new HttpValidationProblemDetails(validation.Errors)
                {
                    Title = "Repository name conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = "A repository with the specified name already exists."
                });
            }

            if (!validation.IsValid)
            {
                return Results.BadRequest(new HttpValidationProblemDetails(validation.Errors)
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred."
                });
            }

            return Results.Created($"/api/v1/repositories/{repository!.Id}", repository);
        });
    }
}
