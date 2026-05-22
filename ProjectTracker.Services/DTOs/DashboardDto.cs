namespace ProjectTracker.Services.DTOs
{
    public class DashboardDto
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int TotalWorkItems { get; set; }
        public int CompletedWorkItems { get; set; }
        public int InProgressWorkItems { get; set; }
        public int ToDoWorkItems { get; set; }
        public int BlockedWorkItems { get; set; }
        public int TotalTeamMembers { get; set; }

        public double CompletionPercentage => TotalWorkItems > 0
            ? Math.Round((double)CompletedWorkItems / TotalWorkItems * 100, 1)
            : 0;

        public List<RecentActivityDto> RecentActivities { get; set; } = new();
        public List<ProjectProgressDto> ProjectProgress { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Project", "WorkItem", "Comment"
        public string Action { get; set; } = string.Empty; // "Created", "Updated", "Completed"
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
    }

    public class ProjectProgressDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double CompletionPercentage { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}