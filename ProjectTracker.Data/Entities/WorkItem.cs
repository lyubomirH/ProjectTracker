using ProjectTracker.Data.Enums;

namespace ProjectTracker.Data.Entities
{
    public class WorkItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
        public WorkItemStatus Status { get; set; } = WorkItemStatus.ToDo;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? EstimatedHours { get; set; }
        public int? ActualHours { get; set; }

        public int ProjectId { get; set; }
        public string? AssigneeId { get; set; }
        public string? CreatedById { get; set; }

        public virtual Project Project { get; set; } = null!;
        public virtual ApplicationUser? Assignee { get; set; }
        public virtual ApplicationUser? CreatedBy { get; set; }
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}