namespace GemApi.DTOs.Request
{
    public class BidFilterRequestDto
    {
        // Search
        public string? Search { get; set; }

        // Organization Filters
        public string? Ministry { get; set; }
        public string? DepartmentName { get; set; }
   
        public string? OrganisationName { get; set; }
        public string? OfficeName { get; set; }

        // Category Filters
        public string? CategoryKey { get; set; }
        public string? CategorySubKey { get; set; }

        // Status Filters
        public bool? Active { get; set; }
        public bool? ClosingSoon { get; set; }
        public bool? Expired { get; set; }

        // Date Filters
       
        //public DateTime? ClosingDateFrom { get; set; }
        //public DateTime? ClosingDateTo { get; set; }

        public DateTime? CardStartDate { get; set; }

        public DateTime? CardEndDate { get; set; }

        // Price Filters
        public decimal? MinEstimatedValue { get; set; }
        public decimal? MaxEstimatedValue { get; set; }
        public decimal? MinEMD { get; set; }
        public decimal? MaxEMD { get; set; }

        // Other Filters
        public string? EvaluationMethod { get; set; }
        public bool? MSEPurchasePreference { get; set; }
        public bool? MIIPurchasePreference { get; set; }

        // Sorting
        public string SortBy { get; set; } = "CardEndDate";
        public bool Descending { get; set; } = false;

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}