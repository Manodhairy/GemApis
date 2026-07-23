namespace GemApi.DTOs.Response
{
    public class BidDetailDto
    {
        public string? BidNumber { get; set; }
        public string? Ministry { get; set; }
        public string? DepartmentName { get; set; }
        public string? OrganisationName { get; set; }
        public string? OfficeName { get; set; }
        public string? ItemCategory { get; set; }
        public decimal? EstimatedBidValue { get; set; }
        public decimal? EmdAmount { get; set; }
        public DateTime? BidDate { get; set; }
        public DateTime? BidEndDateTime { get; set; }
        public string? EvaluationMethod { get; set; }
        public string? PdfUrl { get; set; }
    }
}