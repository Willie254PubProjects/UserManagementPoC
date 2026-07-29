namespace UserManagementPoC.Shared.Authorization.Models;

public class AuthorizationResult
{
    public bool IsAllowed { get; set; }
    public string? Reason { get; set; }
    public static AuthorizationResult Allowed() => new()
    {
        IsAllowed = true
    };
    public static AuthorizationResult Denied(string? reason = null) => new()
    {
        IsAllowed = false,
        Reason = reason
    };

}