using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
    {
        public void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            builder.ToTable("UserPermissions");
            builder.HasKey(up => new { up.PermissionId, up.UserId });
            builder.HasOne(up => up.Permission).WithMany().HasForeignKey(up => up.PermissionId);
            builder.HasOne(up => up.User).WithMany().HasForeignKey(up => up.UserId);
            builder.HasOne(up => up.OrganizationUnit).WithMany().HasForeignKey(up => up.ScopeOrganizationUnitId);
        }
    }
}
