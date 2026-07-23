using GemApi.Data;
using GemApi.DTOs;
using GemApi.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace GemApi.Repository
{
    public class BidRepository:IBidRepository
    {
            private readonly ApplicationDbContext _context;

            public BidRepository(ApplicationDbContext context)
            {
                _context = context;
            }

            private IQueryable<GeMbidExtract> ApplyFilters(
                BidFilterDto filter)
            {
                var query = _context.GeMbidExtracts
                    .AsNoTracking()
                    .AsQueryable();

                // Global search
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    string search = filter.Search.Trim();

                    query = query.Where(x =>
                        (x.BidNumber != null &&
                         x.BidNumber.Contains(search)) ||

                        (x.Ministry != null &&
                         x.Ministry.Contains(search)) ||

                        (x.DepartmentName != null &&
                         x.DepartmentName.Contains(search)) ||

                        (x.OrganisationName != null &&
                         x.OrganisationName.Contains(search)) ||

                        (x.ItemCategory != null &&
                         x.ItemCategory.Contains(search)) ||

                        (x.PrimaryProductCategory != null &&
                         x.PrimaryProductCategory.Contains(search)) ||

                        (x.Boqtitle != null &&
                         x.Boqtitle.Contains(search))
                    );
                }

                if (!string.IsNullOrWhiteSpace(filter.BidNumber))
                {
                    query = query.Where(x =>
                        x.BidNumber != null &&
                        x.BidNumber.Contains(filter.BidNumber));
                }

                if (!string.IsNullOrWhiteSpace(filter.TypeOfBid))
                {
                    query = query.Where(x =>
                        x.TypeOfBid == filter.TypeOfBid);
                }

                if (!string.IsNullOrWhiteSpace(filter.EvaluationMethod))
                {
                    query = query.Where(x =>
                        x.EvaluationMethod != null &&
                        x.EvaluationMethod.Contains(
                            filter.EvaluationMethod));
                }

                if (!string.IsNullOrWhiteSpace(filter.Ministry))
                {
                    query = query.Where(x =>
                        x.Ministry != null &&
                        x.Ministry.Contains(filter.Ministry));
                }

                if (!string.IsNullOrWhiteSpace(filter.DepartmentName))
                {
                    query = query.Where(x =>
                        x.DepartmentName != null &&
                        x.DepartmentName.Contains(
                            filter.DepartmentName));
                }

                if (!string.IsNullOrWhiteSpace(filter.OrganisationName))
                {
                    query = query.Where(x =>
                        x.OrganisationName != null &&
                        x.OrganisationName.Contains(
                            filter.OrganisationName));
                }

                if (!string.IsNullOrWhiteSpace(filter.OfficeName))
                {
                    query = query.Where(x =>
                        x.OfficeName != null &&
                        x.OfficeName.Contains(filter.OfficeName));
                }

                if (!string.IsNullOrWhiteSpace(filter.ItemCategory))
                {
                    query = query.Where(x =>
                        x.ItemCategory != null &&
                        x.ItemCategory.Contains(
                            filter.ItemCategory));
                }

                if (!string.IsNullOrWhiteSpace(
                        filter.PrimaryProductCategory))
                {
                    query = query.Where(x =>
                        x.PrimaryProductCategory != null &&
                        x.PrimaryProductCategory.Contains(
                            filter.PrimaryProductCategory));
                }

                if (!string.IsNullOrWhiteSpace(filter.SimilarCategory))
                {
                    query = query.Where(x =>
                        x.SimilarCategory != null &&
                        x.SimilarCategory.Contains(
                            filter.SimilarCategory));
                }

                if (!string.IsNullOrWhiteSpace(filter.CategoryKey))
                {
                    query = query.Where(x =>
                        x.CategoryKey == filter.CategoryKey);
                }

                if (!string.IsNullOrWhiteSpace(filter.CategorySubKey))
                {
                    query = query.Where(x =>
                        x.CategorySubKey == filter.CategorySubKey);
                }

                // Date filters
                if (filter.BidDateFrom.HasValue)
                {
                    query = query.Where(x =>
                        x.BidDate >= filter.BidDateFrom.Value);
                }

                if (filter.BidDateTo.HasValue)
                {
                    query = query.Where(x =>
                        x.BidDate <= filter.BidDateTo.Value);
                }

                if (filter.EndDateFrom.HasValue)
                {
                    query = query.Where(x =>
                        x.BidEndDateTime >= filter.EndDateFrom.Value);
                }

                if (filter.EndDateTo.HasValue)
                {
                    query = query.Where(x =>
                        x.BidEndDateTime <= filter.EndDateTo.Value);
                }

                if (filter.OpeningDateFrom.HasValue)
                {
                    query = query.Where(x =>
                        x.BidOpeningDateTime >=
                        filter.OpeningDateFrom.Value);
                }

                if (filter.OpeningDateTo.HasValue)
                {
                    query = query.Where(x =>
                        x.BidOpeningDateTime <=
                        filter.OpeningDateTo.Value);
                }

                // Amount filters
                if (filter.MinimumBidValue.HasValue)
                {
                    query = query.Where(x =>
                        x.EstimatedBidValue >=
                        filter.MinimumBidValue.Value);
                }

                if (filter.MaximumBidValue.HasValue)
                {
                    query = query.Where(x =>
                        x.EstimatedBidValue <=
                        filter.MaximumBidValue.Value);
                }

                if (filter.MinimumEmdAmount.HasValue)
                {
                    query = query.Where(x =>
                        x.EmdAmount >= filter.MinimumEmdAmount.Value);
                }

                if (filter.MaximumEmdAmount.HasValue)
                {
                    query = query.Where(x =>
                        x.EmdAmount <= filter.MaximumEmdAmount.Value);
                }

                // Quantity filters
                if (filter.MinimumQuantity.HasValue)
                {
                    query = query.Where(x =>
                        x.TotalQuantity >=
                        filter.MinimumQuantity.Value);
                }

                if (filter.MaximumQuantity.HasValue)
                {
                    query = query.Where(x =>
                        x.TotalQuantity <=
                        filter.MaximumQuantity.Value);
                }

                // Boolean filters
                if (filter.InspectionRequired.HasValue)
                {
                    query = query.Where(x =>
                        x.InspectionRequired ==
                        filter.InspectionRequired.Value);
                }

                if (filter.BidToRAEnabled.HasValue)
                {
                    query = query.Where(x =>
                        x.BidToRaenabled ==
                        filter.BidToRAEnabled.Value);
                } 

                if (filter.MSEPurchasePreference.HasValue)
                {
                    query = query.Where(x =>
                        x.MsepurchasePreference ==
                        filter.MSEPurchasePreference.Value);
                }

                if (filter.MIIPurchasePreference.HasValue)
                {
                    query = query.Where(x =>
                        x.MiipurchasePreference ==
                        filter.MIIPurchasePreference.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Material))
                {
                    query = query.Where(x =>
                        x.Material != null &&
                        x.Material.Contains(filter.Material));
                }

                if (!string.IsNullOrWhiteSpace(filter.Specification))
                {
                    query = query.Where(x =>
                        x.Specification != null &&
                        x.Specification.Contains(
                            filter.Specification));
                }

                if (!string.IsNullOrWhiteSpace(filter.ConsigneeName))
                {
                    query = query.Where(x =>
                        x.ConsigneeName != null &&
                        x.ConsigneeName.Contains(
                            filter.ConsigneeName));
                }

                if (!string.IsNullOrWhiteSpace(
                        filter.ConsigneeAddress))
                {
                    query = query.Where(x =>
                        x.ConsigneeAddress != null &&
                        x.ConsigneeAddress.Contains(
                            filter.ConsigneeAddress));
                }

                return query;
            }

            public async Task<List<GeMbidExtract>> FilterBidsAsync(
                BidFilterDto filter)
            {
                int pageNumber =
                    filter.PageNumber < 1 ? 1 : filter.PageNumber;

                int pageSize =
                    filter.PageSize < 1 || filter.PageSize > 100
                        ? 20
                        : filter.PageSize;

                return await ApplyFilters(filter)
                    .OrderByDescending(x => x.CreatedOn)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            public async Task<int> GetFilteredCountAsync(
                BidFilterDto filter)
            {
                return await ApplyFilters(filter).CountAsync();
            }
        }
    }

