using Microsoft.Extensions.DependencyInjection;

using UserManagementPoC.Shared.Security.Contracts;

using UserManagementPoC.Shared.Security.Services;

namespace UserManagementPoC.Shared.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedSecurity(this IServiceCollection services)
    {
        services.AddScoped<IEncryptionService, AesEncryptionService>();
        return services;

    }
}