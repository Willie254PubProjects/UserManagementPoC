namespace UserManagementPoC.Shared.Authorization.Client;

public class AuthorizationOptions
{
    public string? Authority { get; set; }
    public string ServiceName { get; set; } = "authorization";

}