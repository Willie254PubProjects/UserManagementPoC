using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence
{
    public class AdminDbContext : IdentityDbContext<BshUser, BshRole, string>
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);

        }
    }
}