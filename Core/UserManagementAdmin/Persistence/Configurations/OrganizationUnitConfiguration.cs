using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
    {
        public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
        {
            builder.ToTable("OrganizationUnits");
            builder.HasKey(ou => ou.Id);
            builder.Property(ou => ou.Name).HasMaxLength(200).IsRequired();
            builder.Property(ou => ou.Description).HasMaxLength(500);
            builder.Property(ou => ou.UnitCode).HasMaxLength(50).IsRequired();
            builder.Property(ou => ou.CountryCode).HasMaxLength(10).IsRequired();
            builder.HasOne(ou => ou.Type).WithMany().HasForeignKey(ou => ou.TypeId);
            builder.HasOne(ou => ou.Parent).WithMany(ou => ou.Children).HasForeignKey(ou => ou.ParentId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
