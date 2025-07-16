namespace Core.Models.DTOs.Common
{
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

        public PagedResponse() { }

        public PagedResponse(IEnumerable<T> items, int totalCount, int pageSize, int pageNumber)
        {
            Items = items.ToList();
            TotalCount = totalCount;
            PageSize = pageSize;
            PageNumber = pageNumber;
        }
    }
}