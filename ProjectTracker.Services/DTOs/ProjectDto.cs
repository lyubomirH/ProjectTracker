namespace ProjectTracker.Services.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TeamMembersCount { get; set; }
        public int WorkItemsCount { get; set; }
        public int CompletedWorkItemsCount { get; set; }
        public double CompletionPercentage { get; set; }

        // Navigation properties
        public List<TeamMemberDto> TeamMembers { get; set; } = new();
        public List<WorkItemSummaryDto> WorkItems { get; set; } = new();
    }

    public class CreateProjectDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class UpdateProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class WorkItemSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string AssigneeName { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }
    public class ProjectDropdownDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}