namespace GemApi.DTOs.Response
{
    public class FilterDto
    {
        public List<FilterItemDto> Ministries { get; set; } = new();
        public List<FilterItemDto> Departments { get; set; } = new();
        public List<FilterItemDto> Organisations { get; set; } = new();
        public List<FilterItemDto> Offices { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public StatusCountDto Status { get; set; } = new();
    }

    public class FilterItemDto
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class CategoryDto
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
        public List<SubCategoryDto> SubCategories { get; set; } = new();
    }

    public class SubCategoryDto
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class StatusCountDto
    {
        public int Active { get; set; }
        public int ClosingSoon { get; set; }
        public int Expired { get; set; }
    }
}