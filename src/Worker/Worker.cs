using SbomQualityGate.Application.UseCases;

namespace SbomQualityGate.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ProcessNextValidationJobHandler>();

            var processed = await handler.HandleAsync(stoppingToken);

            if (processed)
            {
                _logger.LogInformation("Processed validation job at {Time}", DateTime.UtcNow);
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}