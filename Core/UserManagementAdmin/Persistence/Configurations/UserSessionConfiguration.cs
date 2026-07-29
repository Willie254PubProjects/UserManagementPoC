using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Persistence.Configurations
{
    public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.ToTable("UserSessions");
            builder.HasKey(us => us.Id);
            builder.HasOne(us => us.User).WithMany().HasForeignKey(us => us.UserId);
            builder.Property(us => us.RemoteIP).HasMaxLength(45);
            builder.Property(us => us.UserAgent).HasMaxLength(500);
            builder.HasIndex(us => us.SecurityVersion).IsUnique();
        }
    }
}