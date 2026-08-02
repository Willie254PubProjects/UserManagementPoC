using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations {
 public class BshRoleConfiguration : IEntityTypeConfiguration<BshRole> {
 public void Configure(EntityTypeBuilder<BshRole> builder) {
     builder.Property(r => r.Description).HasMaxLength(500).IsRequired();
 } 
} 
}