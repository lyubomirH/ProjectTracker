namespace ProjectTracker.Data.Entities
{
    public class Comment
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public int WorkItemId { get; set; }
        public string AuthorId { get; set; } = string.Empty;

        public virtual WorkItem WorkItem { get; set; } = null!;
        public virtual ApplicationUser Author { get; set; } = null!;
    }
}