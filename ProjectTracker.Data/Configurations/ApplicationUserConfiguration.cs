using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Data.Entities;

namespace ProjectTracker.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("Users");

            builder.HasIndex(u => u.Email)
                .HasDatabaseName("IX_Users_Email");

            builder.HasIndex(u => u.FirstName)
                .HasDatabaseName("IX_Users_FirstName");

            builder.HasIndex(u => u.LastName)
                .HasDatabaseName("IX_Users_LastName");

            builder.HasIndex(u => u.IsActive)
                .HasDatabaseName("IX_Users_IsActive");

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.Bio)
                .HasMaxLength(500);

            builder.Property(u => u.Department)
                .HasMaxLength(100);

            builder.Property(u => u.JobTitle)
                .HasMaxLength(100);

            builder.Property(u => u.AvatarUrl)
                .HasMaxLength(500);

            builder.Ignore(u => u.FullName);
        }
    }
}