namespace UserManagementPoC.Shared.Abstractions;

public interface ICurrentUser
{
    string? Id { get; }
    string? Name { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}