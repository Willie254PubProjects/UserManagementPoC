using System.Net.Http.Json;

using Microsoft.Extensions.Configuration;

using UserManagementPoC.Shared.Responses;

using UserManagementPoC.Shared.Security.Models;

namespace UserManagementPoC.Shared.Authorization.Sso;

public class IdentitySsoClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    public IdentitySsoClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }
    public async Task<TokenResponse?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var clientId = _configuration["IdentityClient:ClientId"];
        var clientSecret = _configuration["IdentityClient:ClientSecret"];
        var response = await _http.PostAsJsonAsync("/api/auth/token", new
        {
            code,
            clientId,
            clientSecret
        }, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>(cancellationToken);
        return apiResponse?.Data;
    }
    public async Task<UserInfo?> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("/api/auth/me", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserInfo>>(cancellationToken);
        return apiResponse?.Data;
    }
    public async Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync("/api/auth/logout", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}