namespace GemApi.DTOs.Response
{
    public class BidListDto
    {
        public string? BidNumber { get; set; }
        public string? Ministry { get; set; }
        public string? DepartmentName { get; set; }
        public string? OrganisationName { get; set; }
        public string? OfficeName { get; set; }
        public string? CategoryKey { get; set; }
        public string? CategorySubKey { get; set; }
        public decimal? EstimatedBidValue { get; set; }
        public DateTime? BidEndDateTime { get; set; }
        public string? PdfUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosingSoon { get; set; }
    }
}