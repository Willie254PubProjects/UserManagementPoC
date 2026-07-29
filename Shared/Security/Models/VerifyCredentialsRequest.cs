namespace UserManagementPoC.Shared.Security.Models;

public class VerifyCredentialsRequest
{
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public string Iv { get; set; } = string.Empty;

}