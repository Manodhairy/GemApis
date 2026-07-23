namespace GemApi.DTOs
{
    public class BidFilterDto
    {
        public string? Search { get; set; }

        public string? BidNumber { get; set; }
        public string? TypeOfBid { get; set; }
        public string? EvaluationMethod { get; set; }

        public string? Ministry { get; set; }
        public string? DepartmentName { get; set; }
        public string? OrganisationName { get; set; }
        public string? OfficeName { get; set; }

        public string? ItemCategory { get; set; }
        public string? PrimaryProductCategory { get; set; }
        public string? SimilarCategory { get; set; }
        public string? CategoryKey { get; set; }
        public string? CategorySubKey { get; set; }

        public DateTime? BidDateFrom { get; set; }
        public DateTime? BidDateTo { get; set; }

        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }

        public DateTime? OpeningDateFrom { get; set; }
        public DateTime? OpeningDateTo { get; set; }

        public decimal? MinimumBidValue { get; set; }
        public decimal? MaximumBidValue { get; set; }

        public decimal? MinimumEmdAmount { get; set; }
        public decimal? MaximumEmdAmount { get; set; }

        public int? MinimumQuantity { get; set; }
        public int? MaximumQuantity { get; set; }

        public bool? InspectionRequired { get; set; }
        public bool? BidToRAEnabled { get; set; }
        public bool? MSEPurchasePreference { get; set; }
        public bool? MIIPurchasePreference { get; set; }

        public string? Material { get; set; }
        public string? Specification { get; set; }
        public string? ConsigneeName { get; set; }
        public string? ConsigneeAddress { get; set; }

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}