using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class SubPermissionConfiguration : IEntityTypeConfiguration<SubPermission>
    {
        public void Configure(EntityTypeBuilder<SubPermission> builder)
        {
            builder.ToTable("SubPermissions");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(500);
        }
    }
}
