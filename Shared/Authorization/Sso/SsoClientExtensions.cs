using Microsoft.Extensions.DependencyInjection;

using UserManagementPoC.Shared.Authorization.Client;

namespace UserManagementPoC.Shared.Authorization.Sso;

public static class SsoClientExtensions
{
    public static IServiceCollection AddIdentitySsoClient(this IServiceCollection services, string authority)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<BearerTokenHandler>();
        services.AddHttpClient<IdentitySsoClient>(client =>
        {
            client.BaseAddress = new Uri(authority);

        }).AddHttpMessageHandler<BearerTokenHandler>();
        return services;
    }
}