namespace App.ScopedService;

public sealed class ScopedBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScopedBackgroundService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));

    public ScopedBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ScopedBackgroundService> logger) =>
        (_serviceProvider, _logger) = (serviceProvider, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            $"{nameof(ScopedBackgroundService)} is running.");

        while(await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            await DoWorkAsync(stoppingToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            $"{nameof(ScopedBackgroundService)} is working.");

        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            IScopedProcessingService scopedProcessingService =
                scope.ServiceProvider.GetRequiredService<IScopedProcessingService>();

            await scopedProcessingService.DoWorkAsync(stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            $"{nameof(ScopedBackgroundService)} is stopping.");

        await base.StopAsync(stoppingToken);
    }
}