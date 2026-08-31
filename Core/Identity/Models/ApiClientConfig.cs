namespace UserManagementPoC.Identity.Models;

public class ApiClientConfig
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string[] AllowedReturnUrlHosts { get; set; } = [];
    public string? DefaultReturnUrl { get; set; }
}