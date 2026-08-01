namespace UserManagementPoC.Shared.Abstractions;

public interface ICurrentUser
{
    string? Id { get; }
    string UserName { get; }
    string DisplayName { get; }
    string Email { get; }
    string BankId { get; }
    public string Branchid { get;  }
    public string CountryCode {  get; }
    bool IsAuthenticated { get; }
}