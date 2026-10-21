using AutoMapper;
using GemApi.DTOs.Request;
using GemApi.DTOs.Response;
using GemApi.Models.Entity;
using GemApi.Repository.Interfaces;
using GemApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using System.Net.Http;

namespace GemApi.Services
{
    public class GeMBidService : IGeMBidService
    {
        #region Fields
        private const int ClosingSoonWindowDays = 1;
        private const string DashboardCacheKey = "GeM_Dashboard_Stats_Cache";
        private static readonly TimeSpan DashboardCacheDuration = TimeSpan.FromMinutes(3);

        private readonly IGeMBidRepository _repository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeMBidService> _logger;
        #endregion

        #region Constructor
        public GeMBidService(
            IGeMBidRepository repository,
            IMapper mapper,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<GeMBidService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        #endregion


        #region Xl export method

        public async Task<byte[]> ExportBidsAsync(BidFilterRequestDto request)
        {
            var totalStopwatch =
                System.Diagnostics.Stopwatch.StartNew();

          
            var query = BuildFilteredQuery(request);

           
            query = ApplySorting(query, request);

           

            var now = DateTime.Now;
            var closingSoonUpperBound =
                now.AddDays(ClosingSoonWindowDays);

           
            var exportQuery = query
                .Select(x => new BidExportDto
                {
                    BidNumber = x.BidNumber,
                    Department = x.DepartmentName,
                    Organisation = x.OrganisationName,
                    Location = x.Location,
                    Category = x.CategoryKey,
                    SubCategory = x.CategorySubKey,
                    BidStartDate = x.CardStartDate,
                    BidEndDate = x.CardEndDate,
                    Status =
                        x.CardEndDate == null
                            ? "Unknown"
                            : x.CardEndDate < now
                                ? "Expired"
                            : x.CardEndDate <= closingSoonUpperBound
                                ? "Closing Soon"
                            : "Active"
                })
                .AsNoTracking();

          
            var excelStopwatch =
                System.Diagnostics.Stopwatch.StartNew();

            using var stream = new MemoryStream();

            var asyncData =
                exportQuery.AsAsyncEnumerable();

            await stream.SaveAsAsync(
                asyncData,
                sheetName: "Bids"
            );

            excelStopwatch.Stop();

            
            totalStopwatch.Stop();

            Console.WriteLine(
                $"EXCEL STREAMING TIME: " +
                $"{excelStopwatch.ElapsedMilliseconds} ms");

            Console.WriteLine(
                $"TOTAL EXPORT TIME: " +
                $"{totalStopwatch.ElapsedMilliseconds} ms");

            return stream.ToArray();
        }

        #endregion


        #region GET ALL BIDS WITH FILTERS AND PAGINATION
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

            var now = DateTime.Now;
            var closingSoonUpperBound = now.AddDays(ClosingSoonWindowDays);

            // OPTIMIZATION: Project only the required columns directly in SQL
            // Avoids fetching 80+ columns (NVARCHAR(MAX) & JSON) over the network
            var dtoList = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new BidListDto
                {
                    BidNumber = x.BidNumber,
                    DepartmentName = !string.IsNullOrEmpty(x.DepartmentName) 
                        ? x.DepartmentName 
                        : (!string.IsNullOrEmpty(x.CardDepartment) ? x.CardDepartment : "N/A"),
                    OrganisationName = !string.IsNullOrEmpty(x.OrganisationName) 
                        ? x.OrganisationName 
                        : "N/A",
                    OfficeName = x.OfficeName,
                    Location = !string.IsNullOrEmpty(x.Location) ? x.Location : (!string.IsNullOrEmpty(x.OfficeName) ? x.OfficeName : "-"),
                    ConsigneeName = x.ConsigneeName,
                    CategoryKey = !string.IsNullOrEmpty(x.CategoryKey) ? x.CategoryKey : (!string.IsNullOrEmpty(x.ItemCategory) ? x.ItemCategory : "OTHER"),
                    CategorySubKey = !string.IsNullOrEmpty(x.CategorySubKey) ? x.CategorySubKey : (!string.IsNullOrEmpty(x.CategoryKey) ? x.CategoryKey : "OTHER"),
                    ItemCategory = x.ItemCategory,
                    Boqtitle = x.Boqtitle,
                    CardStartDate = x.CardStartDate,
                    CardEndDate = x.CardEndDate,
                    BidDate = x.BidDate,
                    TotalQuantity = x.TotalQuantity,
                    EstimatedBidValue = x.EstimatedBidValue,
                    EmdAmount = x.EmdAmount,
                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.UpdatedOn,
                    PdfUrl = x.PdfUrl,
                    IsActive = x.CardStartDate <= now && x.CardEndDate >= now,
                    IsClosingSoon = x.CardEndDate >= now && x.CardEndDate <= closingSoonUpperBound
                })
                .ToListAsync();

            var filters = await GetFiltersAsync(request);

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

            var consignees =
    await BuildFilteredQuery(
            request,
            exclude: "Consignee")
        .Where(x => x.ConsigneeName != null)
        .GroupBy(x => x.ConsigneeName)
        .Select(group =>
            new FilterItemDto
            {
                Name = group.Key!,
                Count = group.Count()
            })
        .OrderByDescending(x => x.Count)
        .ToListAsync();

            var locations =
                await BuildFilteredQuery(
                        request,
                        exclude: "Location")
                    .Where(x => !string.IsNullOrWhiteSpace(x.Location))
                    .GroupBy(x => x.Location)
                    .Select(group =>
                        new FilterItemDto
                        {
                            Name = group.Key!,
                            Count = group.Count()
                        })
                    .OrderByDescending(x => x.Count)
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
                Locations = locations,
                Consignees = consignees,
                Categories = categories,
                Status = status
            };
        }
        #endregion


        #region DASHBOARD (Cached in memory for 3 minutes)
        public async Task<DashboardDto> GetDashboardAsync()
        {
            if (_cache.TryGetValue(DashboardCacheKey, out DashboardDto? cachedDashboard) && cachedDashboard != null)
            {
                return cachedDashboard;
            }

            var query = _repository.GetAll();
            var now = DateTime.Now;
            var today = now.Date;
            var closingSoonUpperBound = now.AddDays(ClosingSoonWindowDays);

            var activeQuery = query.Where(x => x.CardStartDate <= now && x.CardEndDate >= now);

            // Fast KPI Counts directly from indexed columns
            var dashboard = new DashboardDto
            {
                TotalBids = await query.CountAsync(),
                ActiveBids = await activeQuery.CountAsync(),
                ClosingSoon = await query.CountAsync(x => x.CardEndDate >= now && x.CardEndDate <= closingSoonUpperBound),
                ExpiredBids = await query.CountAsync(x => x.CardEndDate < now)
            };

            // ---- YEARLY ----
            var yearlyRaw = await activeQuery
                .Where(x => x.CardStartDate != null)
                .GroupBy(x => x.CardStartDate!.Value.Year)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToListAsync();

            dashboard.YearlyBids = yearlyRaw
                .Select(x => new PeriodCountDto { Period = x.Year.ToString(), Count = x.Count })
                .ToList();

            // ---- MONTHLY (ACTIVE bids only) ----
            var monthlyRaw = await activeQuery
                .Where(x => x.CardStartDate != null)
                .GroupBy(x => new { x.CardStartDate!.Value.Year, x.CardStartDate!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            dashboard.MonthlyBids = monthlyRaw
                .Select(x => new PeriodCountDto { Period = $"{x.Year}-{x.Month:00}", Count = x.Count })
                .ToList();

            // ---- WEEKLY (ACTIVE bids only, bounded to last 12 weeks) ----
            var twelveWeeksAgo = today.AddDays(-84);
            var recentStartDates = await activeQuery
                .Where(x => x.CardStartDate != null && x.CardStartDate >= twelveWeeksAgo)
                .Select(x => x.CardStartDate!.Value)
                .ToListAsync();

            dashboard.WeeklyBids = recentStartDates
                .GroupBy(d => new
                {
                    d.Year,
                    Week = System.Globalization.ISOWeek.GetWeekOfYear(d)
                })
                .Select(g => new PeriodCountDto
                {
                    Period = $"{g.Key.Year}-W{g.Key.Week:00}",
                    Count = g.Count()
                })
                .OrderBy(x => x.Period)
                .ToList();

            // ---- Date boundaries for exclusive expiring buckets ----
            var endOfWeek = today.AddDays(7);
            var endOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
            var endOfYear = new DateTime(today.Year, 12, 31);

            dashboard.ExpiringThisWeekCount = await query.CountAsync(x =>
                x.CardEndDate != null && x.CardEndDate >= today && x.CardEndDate <= endOfWeek);

            dashboard.ExpiringThisMonthCount = await query.CountAsync(x =>
                x.CardEndDate != null && x.CardEndDate > endOfWeek && x.CardEndDate <= endOfMonth);

            dashboard.ExpiringThisYearCount = await query.CountAsync(x =>
                x.CardEndDate != null && x.CardEndDate > endOfMonth && x.CardEndDate <= endOfYear);

            // Store in memory cache for 3 minutes
            _cache.Set(DashboardCacheKey, dashboard, DashboardCacheDuration);

            return dashboard;
        }
        #endregion

        #region BUILD FILTER QUERY
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
        (x.BidNumber ?? "").Contains(search)
        ||
        (x.ItemCategory ?? "").Contains(search)
        ||
        (x.Boqtitle ?? "").Contains(search)
        ||
       (x.DepartmentName ?? "").Contains(search)
||
(x.Ministry ?? "").Contains(search)
||
(x.OrganisationName ?? "").Contains(search)
||
(x.ConsigneeName ?? "").Contains(search)
||
(x.Location ?? "").Contains(search)
    );
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

                // WEEKLY / MONTHLY / YEARLY (Active bids published in current week / month / year)
                if (request.Weekly == true)
                {
                    var currentYear = today.Year;
                    var currentWeek = System.Globalization.ISOWeek.GetWeekOfYear(today);
                    var weekStart = System.Globalization.ISOWeek.ToDateTime(currentYear, currentWeek, DayOfWeek.Monday);
                    var weekEnd = weekStart.AddDays(7).AddTicks(-1);

                    query = query.Where(x =>
                        x.CardStartDate <= now &&
                        x.CardEndDate >= now &&
                        x.CardStartDate != null &&
                        x.CardStartDate >= weekStart &&
                        x.CardStartDate <= weekEnd);
                }
                else if (request.Monthly == true)
                {
                    var monthStart = new DateTime(today.Year, today.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                    query = query.Where(x =>
                        x.CardStartDate <= now &&
                        x.CardEndDate >= now &&
                        x.CardStartDate != null &&
                        x.CardStartDate >= monthStart &&
                        x.CardStartDate <= monthEnd);
                }
                else if (request.Yearly == true)
                {
                    var yearStart = new DateTime(today.Year, 1, 1);
                    var yearEnd = yearStart.AddYears(1).AddTicks(-1);

                    query = query.Where(x =>
                        x.CardStartDate <= now &&
                        x.CardEndDate >= now &&
                        x.CardStartDate != null &&
                        x.CardStartDate >= yearStart &&
                        x.CardStartDate <= yearEnd);
                }

                // EXPIRING THIS WEEK / MONTH / YEAR
               
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
            // CONSIGNEE
            if (exclude != "Consignee"
                &&
                !string.IsNullOrWhiteSpace(request.ConsigneeName))
            {
                query = query.Where(x =>
                    x.ConsigneeName ==
                    request.ConsigneeName);
            }

            // LOCATION
            // LOCATION - MULTIPLE LOCATIONS
            if (exclude != "Location"
                && request.Locations != null
                && request.Locations.Count > 0)
            {
                query = query.Where(x =>
                    x.Location != null &&
                    request.Locations.Contains(x.Location));
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
            // BID DATE

            // From = Bid Start Date
            if (request.CardStartDate.HasValue)
            {
                var fromDate = request.CardStartDate.Value.Date;

                query = query.Where(x =>
                    x.CardStartDate >= fromDate);
            }

            // To = Bid End Date
            if (request.CardEndDate.HasValue)
            {
                var toDate = request.CardEndDate.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.CardEndDate < toDate);
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
        #endregion


        #region ApplySorting
        private static IQueryable<GeMbidExtract>
           ApplySorting(
               IQueryable<GeMbidExtract> query,
               BidFilterRequestDto request)
        {
            
            IQueryable<GeMbidExtract> StatusPriorityOrder(bool newestFirst = false)
            {
                var now = DateTime.Now;
                var closingSoonUpperBound =
                    now.AddDays(ClosingSoonWindowDays);

                var ordered = query
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
                        : 3);

                return newestFirst
                    ? ordered
                        .ThenByDescending(x => x.UpdatedOn ?? x.CreatedOn)
                        .ThenByDescending(x => x.Id)
                    : ordered
                        .ThenBy(x => x.CardEndDate);
            }

            switch (request.SortBy?.ToLower())
            {
                case "status":
                    return StatusPriorityOrder(request.Descending);

                case "recentlyupdated":
                case "updatedon":
                case "latest":
                case "newest":
                    return StatusPriorityOrder(newestFirst: true);

                case "biddate":
                    return request.Descending
                        ? query.OrderByDescending(x => x.BidDate)
                        : query.OrderBy(x => x.BidDate);

                case "createdon":
                    return request.Descending
                        ? query.OrderByDescending(x => x.CreatedOn)
                        : query.OrderBy(x => x.CreatedOn);

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

        #endregion

        #region EMAIL NOTIFICATION SUMMARY
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
        #endregion

        #region PDF Retrieval and Streaming Proxy
        public async Task<(byte[]? FileBytes, string FileName, string? ErrorMessage)> GetBidPdfAsync(string bidNumber, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(bidNumber))
            {
                return (null, "", "Bid number or PDF URL must be specified.");
            }

            var trimmedInput = bidNumber.Trim();
            string? targetPdfUrl = null;
            string cleanBidNumber = trimmedInput;

            // Check if input is already a direct/relative URL
            if (trimmedInput.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmedInput.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                targetPdfUrl = trimmedInput;
                cleanBidNumber = Path.GetFileNameWithoutExtension(trimmedInput);
            }
            else if (trimmedInput.StartsWith("/showbidDocument/", StringComparison.OrdinalIgnoreCase))
            {
                targetPdfUrl = $"https://bidplus.gem.gov.in{trimmedInput}";
                cleanBidNumber = Path.GetFileNameWithoutExtension(trimmedInput);
            }
            else
            {
                // Look up bid in database
                var entity = await _repository.GetByBidNumberAsync(trimmedInput);
                if (entity == null)
                {
                    return (null, "", $"Bid '{trimmedInput}' was not found in the database.");
                }

                if (string.IsNullOrWhiteSpace(entity.PdfUrl))
                {
                    return (null, "", $"No PDF document URL is associated with bid '{trimmedInput}'.");
                }

                targetPdfUrl = entity.PdfUrl.Trim();
                if (!targetPdfUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !targetPdfUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    targetPdfUrl = $"https://bidplus.gem.gov.in{(targetPdfUrl.StartsWith('/') ? "" : "/")}{targetPdfUrl}";
                }

                cleanBidNumber = entity.BidNumber ?? trimmedInput;
            }

            var safeBaseName = cleanBidNumber.Replace("/", "_").Replace("\\", "_").Replace(":", "_");
            var fileName = $"{safeBaseName}.pdf";
            var cacheKey = $"pdf_{safeBaseName}";

            // Check memory cache
            if (_cache.TryGetValue(cacheKey, out byte[]? cachedBytes) && cachedBytes != null && cachedBytes.Length > 0)
            {
                return (cachedBytes, fileName, null);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("GemPdfClient");
                using var response = await client.GetAsync(targetPdfUrl, HttpCompletionOption.ResponseHeadersRead, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[PDF Proxy] HTTP {StatusCode} from GeM for URL {Url}", (int)response.StatusCode, targetPdfUrl);
                    return (null, fileName, $"Failed to download tender PDF document: GeM server returned status code {(int)response.StatusCode}.");
                }

                var pdfBytes = await response.Content.ReadAsByteArrayAsync(ct);

                // Verify valid PDF magic bytes: %PDF (0x25, 0x50, 0x44, 0x46)
                if (pdfBytes.Length >= 4 &&
                    pdfBytes[0] == 0x25 &&
                    pdfBytes[1] == 0x50 &&
                    pdfBytes[2] == 0x44 &&
                    pdfBytes[3] == 0x46)
                {
                    _cache.Set(cacheKey, pdfBytes, TimeSpan.FromHours(1));
                    return (pdfBytes, fileName, null);
                }

                _logger.LogWarning("[PDF Proxy] Downloaded data from {Url} did not contain valid PDF magic bytes (Length: {Len})", targetPdfUrl, pdfBytes.Length);
                return (null, fileName, "The tender document could not be retrieved or is not a valid PDF file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PDF Proxy] Exception downloading PDF from {Url} for bid '{Bid}': {Msg}", targetPdfUrl, cleanBidNumber, ex.Message);
                return (null, fileName, $"Failed to download tender PDF document: {ex.Message}");
            }
        }
        #endregion
    }
}