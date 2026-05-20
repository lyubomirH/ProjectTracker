using ProjectTracker.Data.Enums;

namespace ProjectTracker.Data.Entities
{
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string OwnerId { get; set; } = string.Empty;

        public virtual ApplicationUser Owner { get; set; } = null!;
        public virtual ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
        public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    }
}