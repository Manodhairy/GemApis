using GemApi.Services.Interfaces;
using GemApi.Settings;
using Microsoft.Extensions.Options;

namespace GemApi.BackgroundServices;

public class PuneBidAlertBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PuneBidAlertSettings _settings;
    private readonly ILogger<PuneBidAlertBackgroundService> _logger;

    public PuneBidAlertBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<PuneBidAlertSettings> options,
        ILogger<PuneBidAlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Pune Bid Alert Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var puneBidAlertService =
                    scope.ServiceProvider
                        .GetRequiredService<IPuneBidAlertService>();

                await puneBidAlertService
                    .CheckForPuneBidsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred during Pune bid alert check.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(
                        _settings.CheckIntervalMinutes),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Pune Bid Alert Background Service stopped.");
    }
}