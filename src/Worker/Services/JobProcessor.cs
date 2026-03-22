using SbomQualityGate.Application.UseCases;

namespace SbomQualityGate.Worker.Services;

public class JobProcessor(IServiceProvider serviceProvider)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();

            var handler = scope.ServiceProvider
                .GetRequiredService<ProcessNextValidationJobHandler>();

            var processed = await handler.HandleAsync(cancellationToken);

            if (!processed)
            {
                break;
            }
        }
    }
}