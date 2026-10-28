namespace CodeAtlas.Modules.Jobs;

using CodeAtlas.Common.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class JobsModule : IModule
{
    public string Name => "Jobs";

    public void RegisterModule(IServiceCollection services, IConfiguration configuration)
    {
        // Reserved for future background queue definitions, job schedulers, and execution handlers.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/jobs")
            .WithTags("Jobs");

        group.MapGet("/status", () => Results.Ok(new
        {
            Module = Name,
            Status = "Registered",
            Description = "Asynchronous jobs, task scheduling, and background dispatching module boundary."
        }));
    }
}
