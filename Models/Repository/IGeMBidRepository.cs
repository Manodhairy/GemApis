using GemApi.Models.Entity;

namespace GemApi.Repository.Interfaces
{
    public interface IGeMBidRepository
    {
        IQueryable<GeMbidExtract> GetAll();
        Task<GeMbidExtract?> GetByBidNumberAsync(string bidNumber);
        Task AddAsync(GeMbidExtract entity);
        Task AddRangeAsync(IEnumerable<GeMbidExtract> entities);
        Task UpdateAsync(GeMbidExtract entity);
        Task DeleteAsync(GeMbidExtract entity);
        Task<int> SaveChangesAsync();
    }
}