using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class SubsidiaryConfiguration : IEntityTypeConfiguration<Subsidiary>
    {
        public void Configure(EntityTypeBuilder<Subsidiary> builder)
        {
            builder.ToTable("Subsidiaries");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Description).HasMaxLength(500);
            builder.Property(s => s.CountryCode).HasMaxLength(10);
            builder.HasMany(s => s.Branches).WithOne(b => b.Subsidiary).HasForeignKey(b => b.SubsidiaryId);
        }
    }
}