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
    }
}