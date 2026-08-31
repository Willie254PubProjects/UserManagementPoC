namespace UserManagementAdmin.Models.ViewModels;

public class PaginationViewModel
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string Action { get; set; } = "Index";
}