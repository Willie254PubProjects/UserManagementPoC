using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            builder.HasOne(rp => rp.Role).WithMany(r => r.Permissions).HasForeignKey(rp => rp.RoleId);
            builder.HasOne(rp => rp.Permission).WithMany(p => p.Roles).HasForeignKey(rp => rp.PermissionId);
        }
    }
}
