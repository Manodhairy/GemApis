using GemApi.DTOs.Request;
using GemApi.DTOs.Response;

namespace GemApi.Services.Interfaces
{
    public interface IGeMBidService
    {
        Task<PagedResponseDto<List<BidListDto>>> GetBidsAsync(BidFilterRequestDto request);
        Task<BidDetailDto?> GetBidDetailsAsync(string bidNumber);
        Task<FilterDto> GetFiltersAsync(BidFilterRequestDto request);
        Task<DashboardDto> GetDashboardAsync();

        Task<BidNotificationSummaryDto>GetNotificationSummaryAsync(
                int lastProcessedBidId,
                int currentMaximumBidId);
//For Xl eport
        Task<byte[]> ExportBidsAsync(BidFilterRequestDto request);

        // PDF Document Streaming Proxy
        Task<(byte[]? FileBytes, string FileName, string? ErrorMessage)> GetBidPdfAsync(string bidNumber, CancellationToken ct = default);
    }
}