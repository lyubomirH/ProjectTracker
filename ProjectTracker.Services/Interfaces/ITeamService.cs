using ProjectTracker.Services.DTOs;

namespace ProjectTracker.Services.Interfaces
{
    public interface ITeamService
    {
        Task<List<TeamMemberSimpleDto>> GetTeamMembersForDropdownAsync(int projectId);
        Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int projectId);
        Task<TeamMemberDto?> GetTeamMemberAsync(int projectId, string userId);
        Task<TeamMemberDto> AddTeamMemberAsync(int projectId, string userId, string role, string addedByUserId);
        Task<bool> RemoveTeamMemberAsync(int projectId, string userId, string removedByUserId);
        Task<bool> UpdateTeamMemberRoleAsync(int projectId, string userId, string newRole, string updatedByUserId);
        Task<IEnumerable<UserDto>> GetAvailableUsersForProjectAsync(int projectId);
        Task<bool> IsUserTeamMemberAsync(int projectId, string userId);
        Task<bool> IsUserProjectManagerAsync(int projectId, string userId);
        Task<bool> CanUserManageTeamAsync(int projectId, string userId);
        Task<IEnumerable<UserDto>> GetProjectManagersAsync();
        Task<IEnumerable<TeamMemberDto>> GetUserProjectsAsync(string userId);
    }
}