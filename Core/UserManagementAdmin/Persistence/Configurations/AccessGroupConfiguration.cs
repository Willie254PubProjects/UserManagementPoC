using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class AccessGroupConfiguration : IEntityTypeConfiguration<AccessGroup>
    {
        public void Configure(EntityTypeBuilder<AccessGroup> builder)
        {
            builder.ToTable("AccessGroups");
            builder.HasKey(g => g.Id);
            builder.Property(g => g.Name).HasMaxLength(200).IsRequired();
            builder.Property(g => g.Description).HasMaxLength(500);
            builder.HasMany(g => g.Permissions).WithOne(p => p.AccessGroup).HasForeignKey(p => p.AccessGroupId);
            builder.HasMany(g => g.Roles).WithOne(r => r.AccessGroup).HasForeignKey(r => r.AccessGroupId);
        }
    }
}
