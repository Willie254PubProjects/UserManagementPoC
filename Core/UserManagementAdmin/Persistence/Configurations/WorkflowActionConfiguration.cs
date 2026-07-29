using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class WorkflowActionConfiguration : IEntityTypeConfiguration<WorkflowAction>
    {
        public void Configure(EntityTypeBuilder<WorkflowAction> builder)
        {
            builder.ToTable("WorkflowActions");
            builder.HasKey(a => a.ActionId);
            builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
            builder.Property(a => a.Description).HasMaxLength(500);
            builder.HasOne(a => a.Workflow).WithMany(w => w.Actions).HasForeignKey(a => a.WorkflowId);

        }
    }
}