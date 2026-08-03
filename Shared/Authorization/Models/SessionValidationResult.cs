namespace UserManagementPoC.Shared.Authorization.Models;

public class SessionValidationResult
{
    public string UserId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int PermissionVersion { get; set; }
}