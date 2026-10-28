namespace CodeAtlas.Modules.Architecture;

using CodeAtlas.Common.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class ArchitectureModule : IModule
{
    public string Name => "Architecture";

    public void RegisterModule(IServiceCollection services, IConfiguration configuration)
    {
        // Reserved for future architectural models and dependency boundary analyzers.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/architecture")
            .WithTags("Architecture");

        group.MapGet("/status", () => Results.Ok(new
        {
            Module = Name,
            Status = "Registered",
            Description = "Software architecture visualization and boundary enforcement module boundary."
        }));
    }
}
