using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class AccessGroupRoleConfiguration : IEntityTypeConfiguration<AccessGroupRole>
    {
        public void Configure(EntityTypeBuilder<AccessGroupRole> builder)
        {
            builder.ToTable("AccessGroupRoles");
            builder.HasKey(agr => new { agr.AccessGroupId, agr.RoleId });
            builder.HasOne(agr => agr.AccessGroup).WithMany(g => g.Roles).HasForeignKey(agr => agr.AccessGroupId);
            builder.HasOne(agr => agr.Role).WithMany(r => r.AccessGroups).HasForeignKey(agr => agr.RoleId);
        }
    }
}
