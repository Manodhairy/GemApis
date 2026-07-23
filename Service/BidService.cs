using GemApi.DTOs;
using GemApi.Models.Entity;
using GemApi.Repository;

namespace GemApi.Service
{
    public class BidService: IBidService        
    {
        private readonly IBidRepository _repository;

        public BidService(IBidRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<GeMbidExtract>> FilterBidsAsync(
            BidFilterDto filter)
        {
            return await _repository.FilterBidsAsync(filter);
        }

        public async Task<int> GetFilteredCountAsync(
            BidFilterDto filter)
        {
            return await _repository.GetFilteredCountAsync(filter);
        }
    }
}
