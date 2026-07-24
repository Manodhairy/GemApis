namespace GemApi.DTOs.Response
{
    public class BidNotificationSummaryDto
    {
        public int NewRecordCount { get; set; }

        public int TotalRecordCount { get; set; }

        public DateTime? CreatedOnFrom { get; set; }

        public DateTime? CreatedOnTo { get; set; }

        public List<CreatedDateCountDto>
            CreatedDateCounts
        { get; set; } = new();

        public List<CategoryNotificationCountDto>
            CategoryCounts
        { get; set; } = new();
    }

    public class CreatedDateCountDto
    {
        public DateTime Date { get; set; }

        public int Count { get; set; }
    }

    public class CategoryNotificationCountDto
    {
        public string CategoryKey { get; set; } =
            "Not Available";

        public string CategorySubKey { get; set; } =
            "Not Available";

        public int Count { get; set; }
    }
}