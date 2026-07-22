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
    }
}