using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class UserAccessGroupConfiguration : IEntityTypeConfiguration<UserAccessGroup>
    {
        public void Configure(EntityTypeBuilder<UserAccessGroup> builder)
        {
            builder.ToTable("UserAccessGroups");
            builder.HasKey(uag => uag.Id);
            builder.HasOne(uag => uag.AccessGroup).WithMany().HasForeignKey(uag => uag.AccessGroupId);
            builder.HasOne(uag => uag.User).WithMany().HasForeignKey(uag => uag.UserId);
            builder.HasOne(uag => uag.OrganizationUnit).WithMany().HasForeignKey(uag => uag.ScopeOrganizationUnitId);
        }
    }
}
