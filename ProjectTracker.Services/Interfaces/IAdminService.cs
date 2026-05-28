using ProjectTracker.Services.DTOs;

namespace ProjectTracker.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminStatisticsDto> GetStatisticsAsync();
        Task<PaginatedResult<UserAdminDto>> GetUsersAsync(string? searchTerm, int page, int pageSize);
        Task<UserAdminDto?> GetUserByIdAsync(string userId);
        Task<bool> UpdateUserAsync(EditUserDto userDto);
        Task<bool> DeleteUserAsync(string userId, string currentUserId);
        Task<PaginatedResult<AdminProjectDto>> GetProjectsAsync(string? searchTerm, string? status, int page, int pageSize);
        Task<bool> UpdateProjectStatusAsync(int projectId, string status);
        Task<bool> HardDeleteProjectAsync(int projectId);
        Task<List<RoleDto>> GetRolesAsync();
        Task<bool> CreateRoleAsync(string roleName);
        Task<bool> DeleteRoleAsync(string roleId);
    }
}