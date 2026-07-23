using GemApi.DTOs;
using GemApi.Models.Entity;

namespace GemApi.Service
{
    public interface IBidService
    {
        Task<List<GeMbidExtract>> FilterBidsAsync(
            BidFilterDto filter);

        Task<int> GetFilteredCountAsync(
            BidFilterDto filter);
    }
}
