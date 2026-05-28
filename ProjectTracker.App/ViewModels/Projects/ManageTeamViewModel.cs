using ProjectTracker.Services.DTOs;

namespace ProjectTracker.Web.ViewModels.Projects
{
    public class ManageTeamViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public List<TeamMemberDto> TeamMembers { get; set; } = new();
        public List<UserDto> AvailableUsers { get; set; } = new();
        public List<UserDto> ProjectManagers { get; set; } = new();
        public string CurrentUserRole { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; } = new()
        {
            "Developer",
            "Tester",
            "ProjectManager",
            "Viewer"
        };
    }
}