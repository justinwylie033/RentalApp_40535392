namespace RentalApp.Api.Services;

public sealed class OverdueRentalWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OverdueRentalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IRentalWorkflowService>();
                var updated = await service.MarkOverdueAsync(DateTimeOffset.UtcNow, stoppingToken);
                if (updated > 0)
                {
                    logger.LogInformation("Marked {RentalCount} rentals as overdue", updated);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to check overdue rentals");
            }
        }
    }
}
