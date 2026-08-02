using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class BshUserConfiguration : IEntityTypeConfiguration<BshUser>
    {
        public void Configure(EntityTypeBuilder<BshUser> builder)
        {
            builder.HasOne(u => u.DomicileUnit).WithMany().HasForeignKey(u => u.DomicileUnitId);
        }
    }
}
