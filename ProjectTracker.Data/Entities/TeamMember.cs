using ProjectTracker.Data.Enums;

namespace ProjectTracker.Data.Entities
{
    public class TeamMember
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public TeamRole Role { get; set; } = TeamRole.Developer;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public virtual Project Project { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}