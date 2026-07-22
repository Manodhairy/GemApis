namespace GemApi.DTOs.Response
{
    public class PagedResponseDto<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public T Data { get; set; } = default!;
        public FilterDto Filters { get; set; } = new();
    }
}