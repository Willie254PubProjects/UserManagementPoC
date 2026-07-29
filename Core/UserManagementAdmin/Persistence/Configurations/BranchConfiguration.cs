using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable("Branches");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
            builder.Property(b => b.Description).HasMaxLength(500);
            builder.Property(b => b.BranchCode).HasMaxLength(50);
            builder.HasOne(b => b.Subsidiary).WithMany(s => s.Branches).HasForeignKey(b => b.SubsidiaryId);

        }
    }
}