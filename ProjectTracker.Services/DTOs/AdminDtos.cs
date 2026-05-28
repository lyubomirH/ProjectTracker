namespace ProjectTracker.Services.DTOs
{
    public class AdminStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalWorkItems { get; set; }
        public int TotalComments { get; set; }
        public int ActiveUsers { get; set; }
        public List<RecentProjectDto> RecentProjects { get; set; } = new();
        public List<RecentUserDto> RecentUsers { get; set; } = new();
    }

    public class RecentProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RecentUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserAdminDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? Bio { get; set; }
    }

    public class EditUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? Bio { get; set; }
        public List<string> SelectedRoles { get; set; } = new();
    }

    public class AdminProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public int WorkItemsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }
}