using ProjectTracker.Web.ViewModels.WorkItems;

public class ProjectFilterViewModel
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
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