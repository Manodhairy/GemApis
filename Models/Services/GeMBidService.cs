using AutoMapper;
using GemApi.DTOs.Request;
using GemApi.DTOs.Response;
using GemApi.Models.Entity;
using GemApi.Repository.Interfaces;
using GemApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GemApi.Services
{
    public class GeMBidService : IGeMBidService
    {
        private readonly IGeMBidRepository _repository;
        private readonly IMapper _mapper;

        public GeMBidService(
            IGeMBidRepository repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // GET ALL BIDS WITH FILTERS AND PAGINATION
        public async Task<
            PagedResponseDto<List<BidListDto>>>
            GetBidsAsync(
                BidFilterRequestDto request)
        {
            var query = BuildFilteredQuery(request);

            query = ApplySorting(query, request);

            var totalRecords =
                await query.CountAsync();

            var totalPages =
                request.PageSize > 0
                    ? (int)Math.Ceiling(
                        totalRecords /
                        (double)request.PageSize)
                    : 0;

            var entities = await query
                .Skip(
                    (request.PageNumber - 1)
                    * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dtoList =
                _mapper.Map<List<BidListDto>>(
                    entities);

            var filters =
                await GetFiltersAsync(request);

            return new PagedResponseDto<
                List<BidListDto>>
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                Data = dtoList,
                Filters = filters
            };
        }

        // GET BID DETAILS
        public async Task<BidDetailDto?>
            GetBidDetailsAsync(string bidNumber)
        {
            var entity =
                await _repository
                    .GetByBidNumberAsync(
                        bidNumber);

            return entity == null
                ? null
                : _mapper.Map<BidDetailDto>(
                    entity);
        }

        // GET FILTER COUNTS
        public async Task<FilterDto>
            GetFiltersAsync(
                BidFilterRequestDto request)
        {
            var ministries =
                await BuildFilteredQuery(
                        request,
                        exclude: "Ministry")
                    .Where(x =>
                        x.Ministry != null)
                    .GroupBy(x =>
                        x.Ministry)
                    .Select(group =>
                        new FilterItemDto
                        {
                            Name = group.Key!,
                            Count = group.Count()
                        })
                    .OrderByDescending(x =>
                        x.Count)
                    .ToListAsync();

            var departments =
                await BuildFilteredQuery(
                        request,
                        exclude: "Department")
                    .Where(x =>
                        x.DepartmentName != null)
                    .GroupBy(x =>
                        x.DepartmentName)
                    .Select(group =>
                        new FilterItemDto
                        {
                            Name = group.Key!,
                            Count = group.Count()
                        })
                    .OrderByDescending(x =>
                        x.Count)
                    .ToListAsync();

            var organisations =
                await BuildFilteredQuery(
                        request,
                        exclude: "Organisation")
                    .Where(x =>
                        x.OrganisationName != null)
                    .GroupBy(x =>
                        x.OrganisationName)
                    .Select(group =>
                        new FilterItemDto
                        {
                            Name = group.Key!,
                            Count = group.Count()
                        })
                    .OrderByDescending(x =>
                        x.Count)
                    .ToListAsync();

            var offices =
                await BuildFilteredQuery(
                        request,
                        exclude: "Office")
                    .Where(x =>
                        x.OfficeName != null)
                    .GroupBy(x =>
                        x.OfficeName)
                    .Select(group =>
                        new FilterItemDto
                        {
                            Name = group.Key!,
                            Count = group.Count()
                        })
                    .OrderByDescending(x =>
                        x.Count)
                    .ToListAsync();

            var categoryGroups =
                await BuildFilteredQuery(
                        request,
                        exclude: "Category")
                    .Where(x =>
                        x.CategoryKey != null)
                    .GroupBy(x => new
                    {
                        x.CategoryKey,
                        x.CategorySubKey
                    })
                    .Select(group => new
                    {
                        group.Key.CategoryKey,
                        group.Key.CategorySubKey,
                        Count = group.Count()
                    })
                    .ToListAsync();

            var categories = categoryGroups
                .GroupBy(x =>
                    x.CategoryKey)
                .Select(group =>
                    new CategoryDto
                    {
                        Category = group.Key!,

                        Count = group.Sum(
                            x => x.Count),

                        SubCategories = group
                            .Where(x =>
                                x.CategorySubKey
                                != null)
                            .Select(x =>
                                new SubCategoryDto
                                {
                                    Name =
                                        x.CategorySubKey!,

                                    Count = x.Count
                                })
                            .OrderByDescending(
                                x => x.Count)
                            .ToList()
                    })
                .OrderByDescending(x =>
                    x.Count)
                .ToList();

            var statusBase =
                BuildFilteredQuery(
                    request,
                    exclude: "Status");

            var now = DateTime.Now;

            var status = new StatusCountDto
            {
                Active =
                    await statusBase.CountAsync(
                        x =>
                            x.BidEndDateTime
                            >= now),

                ClosingSoon =
                    await statusBase.CountAsync(
                        x =>
                            x.BidEndDateTime
                            >= now
                            &&
                            x.BidEndDateTime
                            <= now.AddDays(3)),

                Expired =
                    await statusBase.CountAsync(
                        x =>
                            x.BidEndDateTime
                            < now)
            };

            return new FilterDto
            {
                Ministries = ministries,
                Departments = departments,
                Organisations = organisations,
                Offices = offices,
                Categories = categories,
                Status = status
            };
        }

        // DASHBOARD
        public async Task<DashboardDto>
            GetDashboardAsync()
        {
            var query = _repository.GetAll();

            var now = DateTime.Now;

            return new DashboardDto
            {
                TotalBids =
                    await query.CountAsync(),

                ActiveBids =
                    await query.CountAsync(
                        x =>
                            x.BidEndDateTime
                            >= now),

                ClosingSoon =
                    await query.CountAsync(
                        x =>
                            x.BidEndDateTime
                            >= now
                            &&
                            x.BidEndDateTime
                            <= now.AddDays(3)),

                ExpiredBids =
                    await query.CountAsync(
                        x =>
                            x.BidEndDateTime
                            < now),

                TotalMinistries =
                    await query
                        .Where(x =>
                            x.Ministry != null)
                        .Select(x =>
                            x.Ministry)
                        .Distinct()
                        .CountAsync(),

                TotalDepartments =
                    await query
                        .Where(x =>
                            x.DepartmentName
                            != null)
                        .Select(x =>
                            x.DepartmentName)
                        .Distinct()
                        .CountAsync(),

                TotalOrganisations =
                    await query
                        .Where(x =>
                            x.OrganisationName
                            != null)
                        .Select(x =>
                            x.OrganisationName)
                        .Distinct()
                        .CountAsync(),

                TotalEstimatedValue =
                    await query.SumAsync(
                        x =>
                            x.EstimatedBidValue
                            ?? 0)
            };
        }

        // BUILD FILTER QUERY
        private IQueryable<GeMbidExtract>
            BuildFilteredQuery(
                BidFilterRequestDto request,
                string? exclude = null)
        {
            IQueryable<GeMbidExtract> query =
                _repository.GetAll();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(
                    request.Search))
            {
                var search =
                    request.Search.Trim();

                query = query.Where(x =>
                    (x.BidNumber ?? "")
                        .Contains(search)
                    ||
                    (x.ItemCategory ?? "")
                        .Contains(search)
                    ||
                    (x.Boqtitle ?? "")
                        .Contains(search)
                    ||
                    (x.DepartmentName ?? "")
                        .Contains(search)
                    ||
                    (x.OrganisationName ?? "")
                        .Contains(search)
                    ||
                    (x.OfficeName ?? "")
                        .Contains(search));
            }

            // STATUS
            if (exclude != "Status")
            {
                if (request.Active == true)
                {
                    query = query.Where(x =>
                        x.BidEndDateTime
                        >= DateTime.Now);
                }

                if (request.ClosingSoon == true)
                {
                    query = query.Where(x =>
                        x.BidEndDateTime
                        >= DateTime.Now
                        &&
                        x.BidEndDateTime
                        <= DateTime.Now.AddDays(3));
                }

                if (request.Expired == true)
                {
                    query = query.Where(x =>
                        x.BidEndDateTime
                        < DateTime.Now);
                }
            }

            // MINISTRY
            if (exclude != "Ministry"
                &&
                !string.IsNullOrWhiteSpace(
                    request.Ministry))
            {
                query = query.Where(x =>
                    x.Ministry ==
                    request.Ministry);
            }

            // DEPARTMENT
            if (exclude != "Department"
                &&
                !string.IsNullOrWhiteSpace(
                    request.DepartmentName))
            {
                query = query.Where(x =>
                    x.DepartmentName ==
                    request.DepartmentName);
            }

            // ORGANISATION
            if (exclude != "Organisation"
                &&
                !string.IsNullOrWhiteSpace(
                    request.OrganisationName))
            {
                query = query.Where(x =>
                    x.OrganisationName ==
                    request.OrganisationName);
            }

            // OFFICE
            if (exclude != "Office"
                &&
                !string.IsNullOrWhiteSpace(
                    request.OfficeName))
            {
                query = query.Where(x =>
                    x.OfficeName ==
                    request.OfficeName);
            }

            // CATEGORY AND SUBCATEGORY
            if (exclude != "Category")
            {
                if (!string.IsNullOrWhiteSpace(
                        request.CategoryKey))
                {
                    query = query.Where(x =>
                        x.CategoryKey ==
                        request.CategoryKey);
                }

                if (!string.IsNullOrWhiteSpace(
                        request.CategorySubKey))
                {
                    query = query.Where(x =>
                        x.CategorySubKey ==
                        request.CategorySubKey);
                }
            }

            // BID DATE
            if (request.BidDateFrom.HasValue)
            {
                query = query.Where(x =>
                    x.BidDate >=
                    request.BidDateFrom);
            }

            if (request.BidDateTo.HasValue)
            {
                query = query.Where(x =>
                    x.BidDate <=
                    request.BidDateTo);
            }

            // CLOSING DATE
            if (request.ClosingDateFrom.HasValue)
            {
                query = query.Where(x =>
                    x.BidEndDateTime >=
                    request.ClosingDateFrom);
            }

            if (request.ClosingDateTo.HasValue)
            {
                query = query.Where(x =>
                    x.BidEndDateTime <=
                    request.ClosingDateTo);
            }

            // ESTIMATED VALUE
            if (request.MinEstimatedValue.HasValue)
            {
                query = query.Where(x =>
                    x.EstimatedBidValue >=
                    request.MinEstimatedValue);
            }

            if (request.MaxEstimatedValue.HasValue)
            {
                query = query.Where(x =>
                    x.EstimatedBidValue <=
                    request.MaxEstimatedValue);
            }

            // EMD
            if (request.MinEMD.HasValue)
            {
                query = query.Where(x =>
                    x.EmdAmount >=
                    request.MinEMD);
            }

            if (request.MaxEMD.HasValue)
            {
                query = query.Where(x =>
                    x.EmdAmount <=
                    request.MaxEMD);
            }

            // EVALUATION METHOD
            if (!string.IsNullOrWhiteSpace(
                    request.EvaluationMethod))
            {
                query = query.Where(x =>
                    x.EvaluationMethod ==
                    request.EvaluationMethod);
            }

            // MSE PREFERENCE
            if (request
                .MSEPurchasePreference
                .HasValue)
            {
                query = query.Where(x =>
                    x.MsepurchasePreference ==
                    request.MSEPurchasePreference);
            }

            // MII PREFERENCE
            if (request
                .MIIPurchasePreference
                .HasValue)
            {
                query = query.Where(x =>
                    x.MiipurchasePreference ==
                    request.MIIPurchasePreference);
            }

            return query;
        }

        // SORTING
        private static IQueryable<GeMbidExtract>
            ApplySorting(
                IQueryable<GeMbidExtract> query,
                BidFilterRequestDto request)
        {
            switch (request.SortBy?.ToLower())
            {
                case "biddate":
                    return request.Descending
                        ? query.OrderByDescending(
                            x => x.BidDate)
                        : query.OrderBy(
                            x => x.BidDate);

                case "estimatedvalue":
                    return request.Descending
                        ? query.OrderByDescending(
                            x => x.EstimatedBidValue)
                        : query.OrderBy(
                            x => x.EstimatedBidValue);

                case "department":
                    return request.Descending
                        ? query.OrderByDescending(
                            x => x.DepartmentName)
                        : query.OrderBy(
                            x => x.DepartmentName);

                default:
                    return request.Descending
                        ? query.OrderByDescending(
                            x => x.BidEndDateTime)
                        : query.OrderBy(
                            x => x.BidEndDateTime);
            }
        }

        // EMAIL NOTIFICATION SUMMARY
        public async Task<BidNotificationSummaryDto>
            GetNotificationSummaryAsync(
                int lastProcessedBidId,
                int currentMaximumBidId)
        {
            var newBidsQuery =
                _repository.GetAll()
                    .Where(x =>
                        x.Id > lastProcessedBidId
                        &&
                        x.Id <= currentMaximumBidId);

            int newRecordCount =
                await newBidsQuery.CountAsync();

            int totalRecordCount =
                await _repository
                    .GetAll()
                    .CountAsync();

            if (newRecordCount == 0)
            {
                return new BidNotificationSummaryDto
                {
                    NewRecordCount = 0,
                    TotalRecordCount =
                        totalRecordCount
                };
            }

            DateTime? createdOnFrom =
                await newBidsQuery.MinAsync(
                    x =>
                        (DateTime?)x.CreatedOn);

            DateTime? createdOnTo =
                await newBidsQuery.MaxAsync(
                    x =>
                        (DateTime?)x.CreatedOn);

            // CREATED ON DATE-WISE COUNT
            var createdDateCounts =
                await newBidsQuery
                    .GroupBy(x =>
                        x.CreatedOn.Date)
                    .Select(group =>
                        new CreatedDateCountDto
                        {
                            Date = group.Key,
                            Count = group.Count()
                        })
                    .OrderBy(x =>
                        x.Date)
                    .ToListAsync();

            // CATEGORY AND SUBCATEGORY COUNT
            var categoryCounts =
                await newBidsQuery
                    .GroupBy(x => new
                    {
                        x.CategoryKey,
                        x.CategorySubKey
                    })
                    .Select(group =>
                        new CategoryNotificationCountDto
                        {
                            CategoryKey =
                                group.Key.CategoryKey
                                ?? "Not Available",

                            CategorySubKey =
                                group.Key.CategorySubKey
                                ?? "Not Available",

                            Count = group.Count()
                        })
                    .OrderByDescending(x =>
                        x.Count)
                    .ToListAsync();

            return new BidNotificationSummaryDto
            {
                NewRecordCount =
                    newRecordCount,

                TotalRecordCount =
                    totalRecordCount,

                CreatedOnFrom =
                    createdOnFrom,

                CreatedOnTo =
                    createdOnTo,

                CreatedDateCounts =
                    createdDateCounts,

                CategoryCounts =
                    categoryCounts
            };
        }
    }
}