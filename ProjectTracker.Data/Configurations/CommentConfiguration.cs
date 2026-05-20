using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectTracker.Data.Entities;

namespace ProjectTracker.Data.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.ToTable("Comments");

            builder.HasOne(c => c.WorkItem)
                .WithMany(w => w.Comments)
                .HasForeignKey(c => c.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.WorkItemId)
                .HasDatabaseName("IX_Comments_WorkItemId");

            builder.HasIndex(c => c.AuthorId)
                .HasDatabaseName("IX_Comments_AuthorId");

            builder.HasIndex(c => c.CreatedAt)
                .HasDatabaseName("IX_Comments_CreatedAt");

            builder.Property(c => c.Content)
                .IsRequired()
                .HasMaxLength(1000);
        }
    }
}