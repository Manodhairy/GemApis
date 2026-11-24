using GemApi.DTOs.Response;

namespace GemApi.Services;

public interface IPuneBidEmailService
{
    Task SendPuneBidAlertAsync(
        IReadOnlyCollection<PuneBidAlertDto> bids,
        CancellationToken cancellationToken);

    Task SendDailySummaryAsync(
        DateTime reportDate,
        IReadOnlyDictionary<string, int> counts,
        CancellationToken cancellationToken);
}