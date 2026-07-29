using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class WorkflowTypeConfiguration : IEntityTypeConfiguration<WorkflowType>
    {
        public void Configure(EntityTypeBuilder<WorkflowType> builder)
        {
            builder.ToTable("WorkflowTypes");
            builder.HasKey(w => w.WorkflowId);
            builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
            builder.Property(w => w.Description).HasMaxLength(500);
            builder.HasMany(w => w.Actions).WithOne(a => a.Workflow).HasForeignKey(a => a.WorkflowId);
        }
    }
}