using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");
            builder.HasKey(p => p.Id);
            builder.HasOne(p => p.Workflow).WithMany().HasForeignKey(p => p.WorkflowId);
            builder.HasOne(p => p.Action).WithMany().HasForeignKey(p => p.ActionId);
            builder.HasOne(p => p.Type).WithMany().HasForeignKey(p => p.TypeId);
            builder.Ignore(p => p.Name);
        }
    }
}