namespace GemApi.DTOs.Response
{
    public class BidExportDto
    {
        public string? BidNumber { get; set; }

        public string? Department { get; set; }

        public string? Organisation { get; set; }

        public string? Location { get; set; }

        public string? Category { get; set; }

        public string? SubCategory { get; set; }

        public DateTime? BidStartDate { get; set; }

        public DateTime? BidEndDate { get; set; }

        public string? Status { get; set; }
    }
}