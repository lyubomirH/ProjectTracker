using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Data.Entities;

namespace ProjectTracker.Data.Configurations
{
    public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
    {
        public void Configure(EntityTypeBuilder<WorkItem> builder)
        {
            builder.ToTable("WorkItems");

            builder.HasOne(w => w.Project)
                .WithMany(p => p.WorkItems)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.Assignee)
                .WithMany(u => u.AssignedWorkItems)
                .HasForeignKey(w => w.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(w => w.CreatedBy)
                .WithMany(u => u.CreatedWorkItems)
                .HasForeignKey(w => w.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(w => w.ProjectId)
                .HasDatabaseName("IX_WorkItems_ProjectId");

            builder.HasIndex(w => w.AssigneeId)
                .HasDatabaseName("IX_WorkItems_AssigneeId");

            builder.HasIndex(w => w.Status)
                .HasDatabaseName("IX_WorkItems_Status");

            builder.HasIndex(w => new { w.ProjectId, w.Status })
                .HasDatabaseName("IX_WorkItems_ProjectId_Status");

            builder.Property(w => w.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(w => w.Description)
                .HasMaxLength(2000);

            builder.Property(w => w.Priority)
                .HasConversion<int>();

            builder.Property(w => w.Status)
                .HasConversion<int>();
        }
    }
}