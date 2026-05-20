namespace ProjectTracker.Web.ViewModels.WorkItems
{
    public class WorkItemDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string AssigneeName { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public int? EstimatedHours { get; set; }
        public int? ActualHours { get; set; }
        public DateTime? CompletedAt { get; set; }

        public List<CommentViewModel> Comments { get; set; } = new();

        public string PriorityBadgeClass => Priority switch
        {
            "Low" => "bg-info",
            "Medium" => "bg-primary",
            "High" => "bg-warning",
            "Critical" => "bg-danger",
            _ => "bg-secondary"
        };

        public string StatusBadgeClass => Status switch
        {
            "ToDo" => "bg-secondary",
            "InProgress" => "bg-primary",
            "CodeReview" => "bg-info",
            "Testing" => "bg-warning",
            "Done" => "bg-success",
            "Blocked" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}