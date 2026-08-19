using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Data.Entities;

namespace ProjectTracker.Data.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("Projects");

            builder.HasQueryFilter(p => !p.IsDeleted);

            builder.HasOne(p => p.Owner)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique index for Project Name to prevent duplicates
            builder.HasIndex(p => p.Name)
                .IsUnique()
                .HasDatabaseName("IX_Projects_Name_Unique");

            builder.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Projects_Status");

            builder.HasIndex(p => p.OwnerId)
                .HasDatabaseName("IX_Projects_OwnerId");

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.Property(p => p.Status)
                .HasConversion<int>();
        }
    }
}