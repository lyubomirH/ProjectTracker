using ProjectTracker.Services.DTOs;

namespace ProjectTracker.Services.Interfaces
{
    public interface ITeamService
    {
        Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int projectId);
        Task<TeamMemberDto> AddTeamMemberAsync(int projectId, string userId, string role, string addedByUserId);
        Task<bool> RemoveTeamMemberAsync(int projectId, string userId, string removedByUserId);
        Task<bool> UpdateTeamMemberRoleAsync(int projectId, string userId, string newRole, string updatedByUserId);
        Task<IEnumerable<UserDto>> GetAvailableUsersForProjectAsync(int projectId);
        Task<bool> IsUserTeamMemberAsync(int projectId, string userId);
    }
}