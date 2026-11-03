namespace PayClaim.DTOs.Common;

public class PagedResponse<T> : ApiResponse<IReadOnlyList<T>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResponse()
    {
    }

    public PagedResponse(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize, string message = "Data retrieved successfully.")
    {
        Success = true;
        StatusCode = 200;
        Message = message;
        Data = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static PagedResponse<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize, string message = "Data retrieved successfully.")
    {
        return new PagedResponse<T>(items, totalCount, pageNumber, pageSize, message);
    }
}
