using Flurl.Http;

using UserManagementPoC.Shared.Responses;

using UserManagementPoC.Shared.Security.Models;

using UserManagementPoC.Shared.Authorization.DTOs;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Identity.Services;

public class UserManagementApiClient : IUserManagementApiClient
{
    private readonly FlurlClient _flurlClient;
    public UserManagementApiClient(HttpClient httpClient)
    {
        _flurlClient = new FlurlClient(httpClient);
    }
    public async Task<UserInfo?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/users", userId)
                                            .AllowAnyHttpStatus()
                                            .GetJsonAsync<ApiResponse<UserInfo>>(cancellationToken: cancellationToken);
        return apiResponse?.Data;
    }
    public async Task<CreateSessionResponse?> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/sessions")
                                            .PostJsonAsync(request, cancellationToken: cancellationToken)
                                            .ReceiveJson<ApiResponse<CreateSessionResponse>>();

        return apiResponse?.Data;
    }
    public async Task<RoleDto[]> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/users", userId, "roles")
                                            .AllowAnyHttpStatus()
                                            .GetJsonAsync<ApiResponse<RoleDto[]>>(cancellationToken: cancellationToken);

        return apiResponse?.Data ?? [];
    }
    public async Task<bool> InvalidateSessionAsync(string securityVersion, CancellationToken cancellationToken = default)
    {
        var response = await _flurlClient.Request("/api/auth/sessions", securityVersion, "invalidate")
                                         .AllowAnyHttpStatus()
                                         .PostAsync(null, cancellationToken: cancellationToken);

        return response.ResponseMessage.IsSuccessStatusCode;
    }
    public async Task<PermissionDto[]> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/users", userId, "permissions")
                                            .AllowAnyHttpStatus()
                                            .GetJsonAsync<ApiResponse<PermissionDto[]>>(cancellationToken: cancellationToken);

        return apiResponse?.Data ?? [];
    }
    public async Task<SessionValidationResult?> GetSessionAsync(string securityVersion, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/sessions", securityVersion)
                                            .AllowAnyHttpStatus()
                                            .GetJsonAsync<ApiResponse<SessionValidationResult>>(cancellationToken: cancellationToken);

        return apiResponse?.Data;
    }
    public async Task<UserInfo?> FindByExternalLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/users/by-login")
                                            .SetQueryParams(new { provider = loginProvider, providerKey })
                                            .AllowAnyHttpStatus()
                                            .GetJsonAsync<ApiResponse<UserInfo>>(cancellationToken: cancellationToken);

        return apiResponse?.Data;
    }
    public async Task<UserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/users/by-email")
                                            .SetQueryParams(new { email })
                                            .AllowAnyHttpStatus()
                                            .GetJsonAsync<ApiResponse<UserInfo>>(cancellationToken: cancellationToken);

        return apiResponse?.Data;
    }
    public async Task<bool> LinkExternalLoginAsync(string userId, string loginProvider, string providerKey, string? providerDisplayName, CancellationToken cancellationToken = default)
    {
        var response = await _flurlClient.Request("/api/auth/users", userId, "logins")
                                         .PostJsonAsync(new { loginProvider, providerKey, providerDisplayName }, cancellationToken: cancellationToken);

        return response.ResponseMessage.IsSuccessStatusCode;
    }
    public async Task<string[]?> ResolveOrgUnitScopeAsync(string value, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/org-units/resolve")
                                            .SetQueryParams(new { value })
                                            .AllowAnyHttpStatus()
                                            .GetJsonAsync<ApiResponse<string[]>>(cancellationToken: cancellationToken);

        return apiResponse?.Data;
    }
    public async Task<string[]?> GetDomicileScopeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/users", userId, "domicile-scope")
                                            .AllowAnyHttpStatus()
                                            .GetJsonAsync<ApiResponse<string[]>>(cancellationToken: cancellationToken);

        return apiResponse?.Data;
    }
}