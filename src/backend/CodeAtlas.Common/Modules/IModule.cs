namespace CodeAtlas.Common.Modules;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Defines the contract for an isolated business module within the modular monolith.
/// Each module encapsulates its own services, data access, and endpoints.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the unique name of the module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers the module's internal and exported services into the dependency injection container.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    void RegisterModule(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Maps the module's HTTP endpoints into the route builder.
    /// </summary>
    /// <param name="endpoints">The route endpoint builder.</param>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
