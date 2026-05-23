namespace ProjectTracker.Web.ViewModels.Projects
{
    public class ProjectListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int TeamMembersCount { get; set; }
        public int WorkItemsCount { get; set; }
        public int CompletedWorkItemsCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public double CompletionPercentage => WorkItemsCount > 0
            ? Math.Round((double)CompletedWorkItemsCount / WorkItemsCount * 100, 1)
            : 0;

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

    public class ProjectIndexViewModel
    {
        public List<ProjectListViewModel> Projects { get; set; } = new();
        public ProjectFilterViewModel Filter { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
    }

    public class ProjectFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public List<string> Statuses { get; } = new() { "All", "Active", "OnHold", "Completed", "Archived", "Cancelled" };
        public List<SortOption> SortOptions { get; } = new()
        {
            new SortOption { Value = "Name", Text = "Name" },
            new SortOption { Value = "CreatedAt", Text = "Created Date" },
            new SortOption { Value = "StartDate", Text = "Start Date" },
            new SortOption { Value = "Status", Text = "Status" },
            new SortOption { Value = "WorkItemsCount", Text = "Task Count" }
        };
    }

    public class SortOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public int StartPage => Math.Max(1, CurrentPage - 2);
        public int EndPage => Math.Min(TotalPages, CurrentPage + 2);
    }
}