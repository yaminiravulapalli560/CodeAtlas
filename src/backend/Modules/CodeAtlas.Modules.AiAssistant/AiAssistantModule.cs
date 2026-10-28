namespace CodeAtlas.Modules.AiAssistant;

using CodeAtlas.Common.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class AiAssistantModule : IModule
{
    public string Name => "AiAssistant";

    public void RegisterModule(IServiceCollection services, IConfiguration configuration)
    {
        // Reserved for future AI client abstractions, RAG pipelines, and prompt management.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/ai-assistant")
            .WithTags("AiAssistant");

        group.MapGet("/status", () => Results.Ok(new
        {
            Module = Name,
            Status = "Registered",
            Description = "Controlled AI assistant abstraction and semantic reasoning module boundary."
        }));
    }
}
