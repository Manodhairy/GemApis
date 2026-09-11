namespace GemApi.Services.Interfaces;

public interface IPuneBidAlertService
{
    Task CheckForPuneBidsAsync(CancellationToken cancellationToken);
}