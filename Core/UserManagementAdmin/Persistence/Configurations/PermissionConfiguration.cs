using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");
            builder.HasKey(p => p.Id);
            builder.HasOne(p => p.SubPermission).WithMany().HasForeignKey(p => p.SubPermissionId);
            builder.HasOne(p => p.Type).WithMany().HasForeignKey(p => p.PermissionTypeId);
            builder.Ignore(p => p.Code);
        }
    }
}
