namespace UserManagementPoC.Identity.Models;

public class TokenRequest
{
    public string Code { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}