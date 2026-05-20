using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Data.Entities;

namespace ProjectTracker.Data.Configurations
{
    public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
    {
        public void Configure(EntityTypeBuilder<TeamMember> builder)
        {
            builder.ToTable("TeamMembers");

            builder.HasIndex(tm => new { tm.ProjectId, tm.UserId })
                .IsUnique()
                .HasDatabaseName("IX_TeamMembers_ProjectId_UserId");

            builder.HasIndex(tm => tm.ProjectId)
                .HasDatabaseName("IX_TeamMembers_ProjectId");

            builder.HasIndex(tm => tm.UserId)
                .HasDatabaseName("IX_TeamMembers_UserId");

            builder.HasIndex(tm => tm.Role)
                .HasDatabaseName("IX_TeamMembers_Role");

            builder.HasOne(tm => tm.Project)
                .WithMany(p => p.TeamMembers)
                .HasForeignKey(tm => tm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tm => tm.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(tm => tm.Role)
                .HasConversion<int>();
        }
    }
}