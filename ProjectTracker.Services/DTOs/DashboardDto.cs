namespace ProjectTracker.Services.DTOs
{
    public class DashboardDto
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int OnHoldProjects { get; set; }

        public int TotalWorkItems { get; set; }
        public int CompletedWorkItems { get; set; }
        public int InProgressWorkItems { get; set; }
        public int ToDoWorkItems { get; set; }
        public int BlockedWorkItems { get; set; }

        public int TotalTeamMembers { get; set; }

        public double CompletionPercentage => TotalWorkItems > 0
            ? Math.Round((double)CompletedWorkItems / TotalWorkItems * 100, 1)
            : 0;

        // Промени от List<RecentActivityDto> на IEnumerable<RecentActivityDto>
        public IEnumerable<RecentActivityDto> RecentActivities { get; set; } = new List<RecentActivityDto>();

        // Промени от List<ProjectProgressDto> на IEnumerable<ProjectProgressDto>
        public IEnumerable<ProjectProgressDto> ProjectProgress { get; set; } = new List<ProjectProgressDto>();

        public List<WorkItemByStatusDto> WorkItemsByStatus { get; set; } = new();
        public List<ProjectByStatusDto> ProjectsByStatus { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class ProjectProgressDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double CompletionPercentage { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? EndDate { get; set; }
    }

    public class WorkItemByStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class ProjectByStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Color { get; set; } = string.Empty;
    }
}