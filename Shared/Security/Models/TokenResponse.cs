namespace UserManagementPoC.Shared.Security.Models;

public class TokenResponse
{
    public string TokenType { get; set; } = "Bearer";
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; }
}