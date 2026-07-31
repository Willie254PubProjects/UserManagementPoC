using Microsoft.Extensions.DependencyInjection;

using UserManagementPoC.Shared.Abstractions;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Helpers;

namespace UserManagementPoC.Shared.Authorization.Client;

using AuthHandler = Microsoft.AspNetCore.Authorization.IAuthorizationHandler;
using AuthPolicyProvider = Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddIdentityAuthorization(this IServiceCollection services, Action<AuthorizationOptions>? configureOptions = null)
    {
        var options = new AuthorizationOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddTransient<BearerTokenHandler>();
        services.AddHttpClient<Contracts.IAuthorizationEvaluator, AuthorizationClient>(client =>
        {
            if (!string.IsNullOrEmpty(options.Authority))
            {
                client.BaseAddress = new Uri(options.Authority);

            }
        }).AddHttpMessageHandler<BearerTokenHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<AuthHandler, AuthorizationEvaluationHandler>();
        services.AddSingleton<AuthPolicyProvider, AuthorizationPolicyProvider>();

        return services;
    }
}