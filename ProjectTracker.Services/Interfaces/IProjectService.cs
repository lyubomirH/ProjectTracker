using ProjectTracker.Services.DTOs;

namespace ProjectTracker.Services.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(string userId, bool isAdmin);
        Task<ProjectDto?> GetProjectByIdAsync(int id, string userId, bool isAdmin);
        Task<ProjectDto> CreateProjectAsync(CreateProjectDto projectDto, string ownerId);
        Task<UpdateProjectDto?> UpdateProjectAsync(UpdateProjectDto projectDto, string userId, bool isAdmin);
        Task<bool> DeleteProjectAsync(int id, string userId, bool isAdmin);
        Task<bool> IsUserInProjectAsync(int projectId, string userId);
        Task<int> GetProjectWorkItemsCountAsync(int projectId);
        Task<double> GetProjectCompletionPercentageAsync(int projectId);
        Task<PaginatedResult<ProjectDto>> GetFilteredProjectsAsync(ProjectFilterDto filter);
        Task<PaginatedResult<WorkItemDto>> GetFilteredWorkItemsAsync(WorkItemFilterDto filter);
    }
}