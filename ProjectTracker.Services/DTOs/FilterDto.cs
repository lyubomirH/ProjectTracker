namespace ProjectTracker.Services.DTOs
{
    public class WorkItemFilterDto
    {
        public int? ProjectId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? AssigneeId { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class ProjectFilterDto
    {
        public string? Status { get; set; }
        public string? OwnerId { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public string? SearchTerm { get; set; }
    }
}