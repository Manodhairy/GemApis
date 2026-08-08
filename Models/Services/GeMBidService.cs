using AutoMapper;
using GemApi.DTOs.Request;
using GemApi.DTOs.Response;
using GemApi.Models.Entity;
using GemApi.Models.Repository;
using GemApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GemApi.Services
{
    public class GeMBidService : IGeMBidService
    {
        // Single source of truth for the "closing soon" window (in days before CardEndDate)
        private const int ClosingSoonWindowDays = 1;

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

            // Populate computed status flags (mapper only maps entity fields 1:1,
            // so status is derived here using CardStartDate/CardEndDate only)
            var now = DateTime.Now;
            foreach (var (dto, entity) in dtoList.Zip(entities, (d, e) => (d, e)))
            {
                dto.IsActive = IsActive(entity.CardStartDate, entity.CardEndDate, now);
                dto.IsClosingSoon = IsClosingSoon(entity.CardEndDate, now);
            }

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
            var closingSoonUpperBound = now.AddDays(ClosingSoonWindowDays);

            var status = new StatusCountDto
            {
                // ACTIVE: CardStartDate <= now <= CardEndDate
                Active =
                    await statusBase.CountAsync(
                        x =>
                            x.CardStartDate <= now
                            &&
                            x.CardEndDate >= now),

                // CLOSING SOON: within ClosingSoonWindowDays of CardEndDate
                ClosingSoon =
                    await statusBase.CountAsync(
                        x =>
                            x.CardEndDate
                            >= now
                            &&
                            x.CardEndDate
                            <= closingSoonUpperBound),

                // EXPIRED: CardEndDate has passed
                Expired =
                    await statusBase.CountAsync(
                        x =>
                            x.CardEndDate
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
            var today = now.Date;
            var closingSoonUpperBound = now.AddDays(ClosingSoonWindowDays);

            // Active-only query — CardStartDate <= now <= CardEndDate.
            // Reused below for the Yearly/Monthly/Weekly breakdown so those
            // cards only ever count bids that are currently Active.
            var activeQuery = query.Where(x =>
                x.CardStartDate <= now
                &&
                x.CardEndDate >= now);

            var dashboard = new DashboardDto
            {
                TotalBids =
                    await query.CountAsync(),
                // ACTIVE: CardStartDate <= now <= CardEndDate
                ActiveBids =
                    await activeQuery.CountAsync(),
                // CLOSING SOON: within ClosingSoonWindowDays of CardEndDate
                ClosingSoon =
                    await query.CountAsync(
                        x =>
                            x.CardEndDate
                            >= now
                            &&
                            x.CardEndDate
                            <= closingSoonUpperBound),
                // EXPIRED: CardEndDate has passed
                ExpiredBids =
                    await query.CountAsync(
                        x =>
                            x.CardEndDate
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

            };

            // ---- YEARLY (ACTIVE bids only, grouped by CardStartDate year) ----
            var yearlyRaw =
                await activeQuery
                    .Where(x =>
                        x.CardStartDate != null)
                    .GroupBy(x =>
                        x.CardStartDate!.Value.Year)
                    .Select(g =>
                        new
                        {
                            Year = g.Key,
                            Count = g.Count()
                        })
                    .OrderBy(x => x.Year)
                    .ToListAsync();

            dashboard.YearlyBids =
                yearlyRaw
                    .Select(x =>
                        new PeriodCountDto
                        {
                            Period = x.Year.ToString(),
                            Count = x.Count
                        })
                    .ToList();

            // ---- MONTHLY (ACTIVE bids only) ----
            var monthlyRaw =
                await activeQuery
                    .Where(x =>
                        x.CardStartDate != null)
                    .GroupBy(x =>
                        new
                        {
                            x.CardStartDate!.Value.Year,
                            x.CardStartDate!.Value.Month
                        })
                    .Select(g =>
                        new
                        {
                            g.Key.Year,
                            g.Key.Month,
                            Count = g.Count()
                        })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

            dashboard.MonthlyBids =
                monthlyRaw
                    .Select(x =>
                        new PeriodCountDto
                        {
                            Period = x.Year + "-" + x.Month.ToString("00"),
                            Count = x.Count
                        })
                    .ToList();

            // ---- WEEKLY (ACTIVE bids only) ----
            // ISOWeek can't be translated to SQL at all, so raw dates must come back first.
            var startDates =
                await activeQuery
                    .Where(x =>
                        x.CardStartDate != null)
                    .Select(x =>
                        x.CardStartDate!.Value)
                    .ToListAsync();

            dashboard.WeeklyBids =
                startDates
                    .GroupBy(d =>
                        new
                        {
                            d.Year,
                            Week = System.Globalization.ISOWeek.GetWeekOfYear(d)
                        })
                    .Select(g =>
                        new PeriodCountDto
                        {
                            Period = $"{g.Key.Year}-W{g.Key.Week:00}",
                            Count = g.Count()
                        })
                    .OrderBy(x => x.Period)
                    .ToList();

            // ---- date boundaries for exclusive expiring buckets ----
            var endOfWeek =
                today.AddDays(7);
            var endOfMonth =
                new DateTime(
                    today.Year,
                    today.Month,
                    DateTime.DaysInMonth(today.Year, today.Month));
            var endOfYear =
                new DateTime(today.Year, 12, 31);

            // ---- EXPIRING THIS WEEK COUNT ----
            dashboard.ExpiringThisWeekCount =
                await query.CountAsync(x =>
                    x.CardEndDate != null
                    &&
                    x.CardEndDate >= today
                    &&
                    x.CardEndDate <= endOfWeek);

            // ---- EXPIRING THIS MONTH COUNT (after week, till end of month) ----
            dashboard.ExpiringThisMonthCount =
                await query.CountAsync(x =>
                    x.CardEndDate != null
                    &&
                    x.CardEndDate > endOfWeek
                    &&
                    x.CardEndDate <= endOfMonth);

            // ---- EXPIRING THIS YEAR COUNT (after month, till end of year) ----
            dashboard.ExpiringThisYearCount =
                await query.CountAsync(x =>
                    x.CardEndDate != null
                    &&
                    x.CardEndDate > endOfMonth
                    &&
                    x.CardEndDate <= endOfYear);

            return dashboard;
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
            // NOTE: status is derived ONLY from CardStartDate / CardEndDate.
            if (exclude != "Status")
            {
                var now = DateTime.Now;
                var today = now.Date;
                var closingSoonUpperBound = now.AddDays(ClosingSoonWindowDays);

                if (request.Active == true)
                {
                    query = query.Where(x =>
                        x.CardStartDate <= now
                        &&
                        x.CardEndDate >= now);
                }

                if (request.ClosingSoon == true)
                {
                    query = query.Where(x =>
                        x.CardEndDate
                        >= now
                        &&
                        x.CardEndDate
                        <= closingSoonUpperBound);
                }

                if (request.Expired == true)
                {
                    query = query.Where(x =>
                        x.CardEndDate
                        < now);
                }

                // EXPIRING THIS WEEK / MONTH / YEAR
                // Mutually exclusive buckets — same logic as GetDashboardAsync,
                // so a bid appearing in "week" never appears in "month" or "year", etc.
                if (request.ExpiringThisWeek == true
                    ||
                    request.ExpiringThisMonth == true
                    ||
                    request.ExpiringThisYear == true)
                {
                    var endOfWeek =
                        today.AddDays(7);
                    var endOfMonth =
                        new DateTime(
                            today.Year,
                            today.Month,
                            DateTime.DaysInMonth(today.Year, today.Month));
                    var endOfYear =
                        new DateTime(today.Year, 12, 31);

                    if (request.ExpiringThisWeek == true)
                    {
                        query = query.Where(x =>
                            x.CardEndDate != null
                            &&
                            x.CardEndDate >= today
                            &&
                            x.CardEndDate <= endOfWeek);
                    }
                    else if (request.ExpiringThisMonth == true)
                    {
                        query = query.Where(x =>
                            x.CardEndDate != null
                            &&
                            x.CardEndDate > endOfWeek
                            &&
                            x.CardEndDate <= endOfMonth);
                    }
                    else if (request.ExpiringThisYear == true)
                    {
                        query = query.Where(x =>
                            x.CardEndDate != null
                            &&
                            x.CardEndDate > endOfMonth
                            &&
                            x.CardEndDate <= endOfYear);
                    }
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
            if (request.CardStartDate.HasValue)
            {
                query = query.Where(x =>
                    x.CardStartDate >=
                    request.CardStartDate);
            }

            if (request.CardEndDate.HasValue)
            {
                query = query.Where(x =>
                    x.CardStartDate <=
                    request.CardEndDate);
            }

            //// CLOSING DATE
            //if (request.CardEndDate.HasValue)
            //{
            //    query = query.Where(x =>
            //        x.CardEndDate >=
            //        request.CardEndDate);
            //}

            //if (request.ClosingDateTo.HasValue)
            //{
            //    query = query.Where(x =>
            //        x.CardEndDate <=
            //        request.ClosingDateTo);
            //}

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

        private static IQueryable<GeMbidExtract>
           ApplySorting(
               IQueryable<GeMbidExtract> query,
               BidFilterRequestDto request)
        {
            // Shared status-priority ordering: Closing Soon (0) -> Active (1) -> Expired (2) -> Other (3).
            // Used both when the caller explicitly asks for sortBy=status AND as the
            // default ordering when no sortBy is supplied at all.
            IQueryable<GeMbidExtract> StatusPriorityOrder()
            {
                var now = DateTime.Now;
                var closingSoonUpperBound =
                    now.AddDays(ClosingSoonWindowDays);

                return query
                    .OrderBy(x =>
                        // Closing Soon = 0
                        (x.CardEndDate >= now &&
                         x.CardEndDate <= closingSoonUpperBound)
                            ? 0

                        // Active = 1
                        : (x.CardStartDate <= now &&
                           x.CardEndDate >= now)
                            ? 1

                        // Expired = 2
                        : (x.CardEndDate < now)
                            ? 2

                        // Other = 3
                        : 3)
                    .ThenBy(x => x.CardEndDate);
            }

            switch (request.SortBy?.ToLower())
            {
                case "status":

                    return StatusPriorityOrder();


                case "biddate":

                    return request.Descending
                        ? query.OrderByDescending(x => x.BidDate)
                        : query.OrderBy(x => x.BidDate);


                case "estimatedvalue":

                    return request.Descending
                        ? query.OrderByDescending(x => x.EstimatedBidValue)
                        : query.OrderBy(x => x.EstimatedBidValue);


                case "department":

                    return request.Descending
                        ? query.OrderByDescending(x => x.DepartmentName)
                        : query.OrderBy(x => x.DepartmentName);


                // No sortBy supplied -> default to Closing Soon -> Active -> Expired.
                default:

                    return StatusPriorityOrder();
            }
        }
        // STATUS HELPERS (CardStartDate / CardEndDate only)
        private static bool IsActive(
            DateTime? cardStartDate,
            DateTime? cardEndDate,
            DateTime now)
            => cardStartDate <= now && cardEndDate >= now;

        private static bool IsClosingSoon(
            DateTime? cardEndDate,
            DateTime now)
            => cardEndDate >= now
               && cardEndDate <= now.AddDays(ClosingSoonWindowDays);

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