using SbomQualityGate.Worker.Services;

namespace SbomQualityGate.Worker;

public class Worker(
    ILogger<Worker> logger,
    JobProcessor processor,
    PostgresNotificationListener listener)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await listener.StartAsync(stoppingToken);

        logger.LogInformation("Worker started. Listening for notifications...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dbWaitTask = listener.WaitAsync(stoppingToken);
                var channelWaitTask = listener.Reader.WaitToReadAsync(stoppingToken).AsTask();
                var delayTask = Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                var completed = await Task.WhenAny(dbWaitTask, channelWaitTask, delayTask);

                if (completed == channelWaitTask && await channelWaitTask)
                {
                    while (listener.Reader.TryRead(out _)) { }

                    logger.LogInformation("Notification received - processing jobs");
                    await processor.ProcessAsync(stoppingToken);
                }
                else if (completed == delayTask)
                {
                    logger.LogInformation("Fallback polling triggered");
                    await processor.ProcessAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker shutting down");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in worker loop");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}