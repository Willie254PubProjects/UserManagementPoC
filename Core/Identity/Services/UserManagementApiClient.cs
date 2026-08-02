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
    public async Task<VerifyCredentialsResponse?> VerifyCredentialsAsync(VerifyCredentialsRequest request, CancellationToken cancellationToken = default)
    {
        var apiResponse = await _flurlClient.Request("/api/auth/verify-credentials")
                                            .PostJsonAsync(request, cancellationToken: cancellationToken)
                                            .ReceiveJson<ApiResponse<VerifyCredentialsResponse>>();
        return apiResponse?.Data;
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
}