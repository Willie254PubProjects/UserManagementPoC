namespace UserManagementPoC.Shared.Security.Models;

public class VerifyCredentialsResponse
{
    public bool Success { get; set; }
    public UserInfo? User { get; set; }
    public string? ErrorMessage { get; set; }
}