namespace UserManagementPoC.Shared.Models;

public class PagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<T> Items { get; set; } = [];
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

}