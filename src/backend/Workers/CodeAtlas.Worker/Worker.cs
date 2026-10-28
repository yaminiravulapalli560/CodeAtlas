namespace CodeAtlas.Worker;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background worker host foundation for asynchronously processing code analysis and jobs.
/// Designed to run either in-process during local development or extractable to a separate container/service.
/// </summary>
public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CodeAtlas Analysis Worker initialized and idling in standby mode.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Placeholder for dequeuing repository analysis tasks
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        logger.LogInformation("CodeAtlas Analysis Worker is stopping.");
    }
}
