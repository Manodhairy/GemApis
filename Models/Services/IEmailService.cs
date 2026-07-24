using GemApi.DTOs.Response;

namespace GemApi.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendBidNotificationAsync(
            BidNotificationSummaryDto summary,
            int minimumRecordCount);
    }
}