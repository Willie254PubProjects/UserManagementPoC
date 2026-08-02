using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class AccessGroupPermissionConfiguration : IEntityTypeConfiguration<AccessGroupPermission>
    {
        public void Configure(EntityTypeBuilder<AccessGroupPermission> builder)
        {
            builder.ToTable("AccessGroupPermissions");
            builder.HasKey(agp => new { agp.AccessGroupId, agp.PermissionId });
            builder.HasOne(agp => agp.AccessGroup).WithMany(g => g.Permissions).HasForeignKey(agp => agp.AccessGroupId);
            builder.HasOne(agp => agp.Permission).WithMany(p => p.AccessGroups).HasForeignKey(agp => agp.PermissionId);
        }
    }
}
