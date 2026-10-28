namespace CodeAtlas.Modules.Identity;

using CodeAtlas.Common.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void RegisterModule(IServiceCollection services, IConfiguration configuration)
    {
        // Reserved for future Identity services, user stores, and auth tokens.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity")
            .WithTags("Identity");

        group.MapGet("/status", () => Results.Ok(new
        {
            Module = Name,
            Status = "Registered",
            Description = "User identity, authentication, and authorization module boundary."
        }));
    }
}
