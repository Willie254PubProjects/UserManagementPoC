using Flurl.Http;

using UserManagementPoC.Shared.Responses;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Shared.Authorization.Client;

internal class AuthorizationClient : IAuthorizationEvaluator
{
    private readonly FlurlClient _flurlClient;
    public AuthorizationClient(HttpClient httpClient)
    {
        _flurlClient = new FlurlClient(httpClient);

    }
    public async Task<AuthorizationResult> EvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiResponse = await _flurlClient.Request("/api/authorization/evaluate").PostJsonAsync(context, cancellationToken: cancellationToken).ReceiveJson<ApiResponse<AuthorizationResult>>();
            return apiResponse?.Data ?? AuthorizationResult.Denied("Invalid response");

        }
        catch (FlurlHttpException)
        {
            return AuthorizationResult.Denied("Authorization service denied the request");

        }
        catch
        {
            return AuthorizationResult.Denied("Authorization service unavailable");

        }
    }
}