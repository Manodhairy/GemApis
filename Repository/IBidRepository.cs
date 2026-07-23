using GemApi.DTOs;
using GemApi.Models.Entity;

namespace GemApi.Repository
{
    public interface IBidRepository
    {
        Task<List<GeMbidExtract>> FilterBidsAsync(
          BidFilterDto filter);

        Task<int> GetFilteredCountAsync(
            BidFilterDto filter);
    }
}
