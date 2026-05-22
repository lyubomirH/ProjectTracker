namespace ProjectTracker.Services.DTOs
{
    public class WorkItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? AssigneeId { get; set; }
        public string AssigneeName { get; set; } = string.Empty;
        public string CreatedById { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? EstimatedHours { get; set; }
        public int? ActualHours { get; set; }
        public int CommentsCount { get; set; }

        // Helper properties
        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date && Status != "Done";
        public bool IsCompleted => Status == "Done";
    }

    public class CreateWorkItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "ToDo";
        public int ProjectId { get; set; }
        public string? AssigneeId { get; set; }
        public DateTime? DueDate { get; set; }
        public int? EstimatedHours { get; set; }
    }

    public class UpdateWorkItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssigneeId { get; set; }
        public DateTime? DueDate { get; set; }
        public int? EstimatedHours { get; set; }
        public int? ActualHours { get; set; }
    }

    public class UpdateWorkItemStatusDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}