using GemApi.DTOs.Response;

namespace GemApi.Services;

public interface IPuneBidEmailService
{
    Task SendPuneBidAlertAsync(
        IReadOnlyCollection<PuneBidAlertDto> bids,
        CancellationToken cancellationToken);
}