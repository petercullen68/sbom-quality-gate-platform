using SbomQualityGate.Worker.Services;

namespace SbomQualityGate.Worker;

public class Worker(
    ILogger<Worker> logger,
    JobProcessor processor,
    PostgresNotificationListener listener)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> WorkerStarted =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(Worker)),
            "Worker started. Listening for notifications...");

    private static readonly Action<ILogger, Exception?> NotificationReceived =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2, nameof(Worker)),
            "Notification received - processing jobs");
    
    private static readonly Action<ILogger, Exception?> FallbackTriggered =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(3, nameof(Worker)),
            "Fallback polling triggered");

    private static readonly Action<ILogger, Exception?> WorkerShuttingDown =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(4, nameof(Worker)),
            "Worker shutting down");
    
    private static readonly Action<ILogger, Exception?> WorkerError =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(5, nameof(Worker)),
            "Error in worker loop");

    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await listener.StartAsync(stoppingToken);

        WorkerStarted(logger, null);;

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

                    NotificationReceived(logger, null);
                    await processor.ProcessAsync(stoppingToken);
                }
                else if (completed == delayTask)
                {
                    FallbackTriggered(logger, null);
                    await processor.ProcessAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                WorkerShuttingDown(logger, null);
            }
            catch (Exception ex)
            {
                WorkerError(logger, ex);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
