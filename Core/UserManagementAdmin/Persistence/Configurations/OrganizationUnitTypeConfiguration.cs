using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class OrganizationUnitTypeConfiguration : IEntityTypeConfiguration<OrganizationUnitType>
    {
        public void Configure(EntityTypeBuilder<OrganizationUnitType> builder)
        {
            builder.ToTable("OrganizationUnitTypes");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
            builder.Property(t => t.Description).HasMaxLength(500);
        }
    }
}
