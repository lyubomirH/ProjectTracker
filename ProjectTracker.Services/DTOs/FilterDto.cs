namespace ProjectTracker.Services.DTOs
{
    public class WorkItemFilterDto
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
        public string UserId { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }

    public class ProjectFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string UserId { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}