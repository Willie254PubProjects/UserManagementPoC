namespace UserManagementPoC.Shared.Security.Models;

public class CreateSessionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? RemoteIp { get; set; }
    public string? UserAgent { get; set; }
}