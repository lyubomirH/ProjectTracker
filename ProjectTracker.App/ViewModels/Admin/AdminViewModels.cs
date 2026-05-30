using System.ComponentModel.DataAnnotations;
using ProjectTracker.Web.ViewModels.Shared;

namespace ProjectTracker.Web.ViewModels.Admin
{
    public class AdminStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalWorkItems { get; set; }
        public int TotalComments { get; set; }
        public int ActiveUsers { get; set; }

        public List<RecentProjectViewModel> RecentProjects { get; set; } = new();
        public List<RecentUserViewModel> RecentUsers { get; set; } = new();
    }

    public class RecentProjectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RecentUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserAdminViewModel
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
    }

    public class UserListViewModel
    {
        public List<UserAdminViewModel> Users { get; set; } = new();
        public string? SearchTerm { get; set; }
        public PaginationViewModel Pagination { get; set; } = new();
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [Display(Name = "Bio")]
        [DataType(DataType.MultilineText)]
        public string? Bio { get; set; }

        public List<string> CurrentRoles { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
        public List<string> SelectedRoles { get; set; } = new();

        public string FullName => $"{FirstName} {LastName}";
    }

    public class AdminProjectViewModel
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

    public class AdminProjectListViewModel
    {
        public List<AdminProjectViewModel> Projects { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public PaginationViewModel Pagination { get; set; } = new();
        public List<string> Statuses { get; set; } = new();
    }

    public class RoleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }

    public class RoleListViewModel
    {
        public List<RoleViewModel> Roles { get; set; } = new();
        public int TotalUsers { get; set; }
    }
}