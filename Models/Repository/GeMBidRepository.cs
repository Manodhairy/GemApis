using GemApi.Data;
using GemApi.Models.Entity;
using GemApi.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GemApi.Repository
{
    public class GeMBidRepository : IGeMBidRepository
    {
        private readonly ApplicationDbContext _context;

        public GeMBidRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<GeMbidExtract> GetAll()
        {
            return _context.GeMbidExtracts.AsNoTracking();
        }

        public async Task<GeMbidExtract?> GetByBidNumberAsync(string bidNumber)
        {
            return await _context.GeMbidExtracts
                .FirstOrDefaultAsync(x => x.BidNumber == bidNumber);
        }

        public async Task AddAsync(GeMbidExtract entity)
        {
            await _context.GeMbidExtracts.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<GeMbidExtract> entities)
        {
            await _context.GeMbidExtracts.AddRangeAsync(entities);
        }

        public Task UpdateAsync(GeMbidExtract entity)
        {
            _context.GeMbidExtracts.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(GeMbidExtract entity)
        {
            _context.GeMbidExtracts.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}