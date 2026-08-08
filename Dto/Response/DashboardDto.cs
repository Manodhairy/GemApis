namespace GemApi.DTOs.Response
{
    public class DashboardDto
    {
        public int TotalBids { get; set; }
        public int ActiveBids { get; set; }
        public int ClosingSoon { get; set; }
        public int ExpiredBids { get; set; }
        public int TotalMinistries { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalOrganisations { get; set; }

        // Time-bucketed counts (bids started per year/month/week)
        public List<PeriodCountDto> YearlyBids { get; set; } = new();
        public List<PeriodCountDto> MonthlyBids { get; set; } = new();
        public List<PeriodCountDto> WeeklyBids { get; set; } = new();

        // Expiring counts only — click-through to GET /api/gembids?ExpiringThisWeek=true etc. for the list
        public int ExpiringThisWeekCount { get; set; }
        public int ExpiringThisMonthCount { get; set; }
        public int ExpiringThisYearCount { get; set; }
    }

    public class PeriodCountDto
    {
        public string Period { get; set; } = default!; // e.g. "2026", "2026-08", "2026-W32"
        public int Count { get; set; }
    }
}