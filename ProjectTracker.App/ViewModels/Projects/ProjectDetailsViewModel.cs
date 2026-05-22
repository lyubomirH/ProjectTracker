namespace ProjectTracker.Web.ViewModels.Projects
{
    public class ProjectDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<TeamMemberViewModel> TeamMembers { get; set; } = new();
        public List<WorkItemSummaryViewModel> WorkItems { get; set; } = new();

        public string StatusBadgeClass => Status switch
        {
            "Active" => "bg-success",
            "OnHold" => "bg-warning",
            "Completed" => "bg-info",
            "Archived" => "bg-secondary",
            "Cancelled" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public class TeamMemberViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class WorkItemSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string AssigneeName { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }

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

        public string PriorityBadgeClass => Priority switch
        {
            "Low" => "bg-info",
            "Medium" => "bg-primary",
            "High" => "bg-warning",
            "Critical" => "bg-danger",
            _ => "bg-secondary"
        };
    }
}