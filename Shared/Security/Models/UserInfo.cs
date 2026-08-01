using UserManagementPoC.Shared.Abstractions;

namespace UserManagementPoC.Shared.Security.Models;

public class UserInfo : ICurrentUser
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName => $"{FirstName} {LastName}";
    public string BankId { get; set; }
    public string BranchId { get; set; }
    public string CountryCode {  get; set; }
    public bool IsAuthenticated {  get; set; }
}