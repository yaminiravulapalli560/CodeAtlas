namespace CodeAtlas.Common.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ModuleExtensions
{
    public static IServiceCollection AddConfiguredModules(
        this IServiceCollection services,
        IConfiguration configuration,
        IEnumerable<IModule> modules)
    {
        var registeredModules = modules.ToList();
        services.AddSingleton<IReadOnlyCollection<IModule>>(registeredModules);

        foreach (var module in registeredModules)
        {
            module.RegisterModule(services, configuration);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapConfiguredModules(
        this IEndpointRouteBuilder endpoints,
        IEnumerable<IModule> modules)
    {
        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
