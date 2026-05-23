using ProjectTracker.Web.ViewModels.Projects;

namespace ProjectTracker.Web.ViewModels.WorkItems
{
    public class WorkItemListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string AssigneeName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
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

        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date && Status != "Done";
    }

    public class WorkItemIndexViewModel
    {
        public List<WorkItemListViewModel> WorkItems { get; set; } = new();
        public WorkItemFilterViewModel Filter { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public List<ProjectDropdownViewModel> Projects { get; set; } = new();
    }

    public class WorkItemFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public int? ProjectId { get; set; }
        public string? AssigneeId { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public List<string> Statuses { get; } = new() { "All", "ToDo", "InProgress", "CodeReview", "Testing", "Done", "Blocked" };
        public List<string> Priorities { get; } = new() { "All", "Low", "Medium", "High", "Critical" };
        public List<SortOption> SortOptions { get; } = new()
        {
            new SortOption { Value = "Title", Text = "Title" },
            new SortOption { Value = "CreatedAt", Text = "Created Date" },
            new SortOption { Value = "DueDate", Text = "Due Date" },
            new SortOption { Value = "Priority", Text = "Priority" },
            new SortOption { Value = "Status", Text = "Status" }
        };
    }

    public class ProjectDropdownViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}