using Microsoft.AspNetCore.Identity;

namespace ProjectTracker.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        public string FullName => $"{FirstName} {LastName}";

        public virtual ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
        public virtual ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
        public virtual ICollection<WorkItem> AssignedWorkItems { get; set; } = new List<WorkItem>();
        public virtual ICollection<WorkItem> CreatedWorkItems { get; set; } = new List<WorkItem>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}