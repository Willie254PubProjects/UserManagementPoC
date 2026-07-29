using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

namespace UserManagementPoC.Shared.Repositories
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRepositories<TContext>(this IServiceCollection services) where TContext : DbContext
        {
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;

        }
    }
}