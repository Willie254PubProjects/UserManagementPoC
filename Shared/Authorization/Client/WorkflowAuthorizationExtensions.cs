using Microsoft.Extensions.DependencyInjection;

using UserManagementPoC.Shared.Abstractions;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Helpers;

namespace UserManagementPoC.Shared.Authorization.Client;

using MicrosoftAuthorizationService = Microsoft.AspNetCore.Authorization.IAuthorizationHandler;
using MicrosoftAuthPolicyProvider = Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider;
public static class WorkflowAuthorizationExtensions
{
    public static IServiceCollection AddWorkflowAuthorization(this IServiceCollection services, Action<WorkflowAuthorizationOptions>? configureOptions = null)
    {
        var options = new WorkflowAuthorizationOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddHttpClient<IAuthorizationEvaluator, AuthorizationClient>(client =>
        {
            if (!string.IsNullOrEmpty(options.Authority))
            {
                client.BaseAddress = new Uri(options.Authority);

            }
        });
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<MicrosoftAuthorizationService, WorkflowAuthorizationHandler>();
        services.AddSingleton<MicrosoftAuthPolicyProvider, WorkflowAuthorizationPolicyProvider>();
        return services;

    }
}