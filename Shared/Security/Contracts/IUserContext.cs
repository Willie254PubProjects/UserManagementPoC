namespace UserManagementPoC.Shared.Security.Contracts;

public interface IUserContext
{
    string? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}