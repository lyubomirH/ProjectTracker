using ProjectTracker.Services.DTOs;

namespace ProjectTracker.Services.Interfaces
{
    public interface IWorkItemService
    {
        Task<IEnumerable<WorkItemDto>> GetWorkItemsAsync(int? projectId, string userId, bool isAdmin);
        Task<WorkItemDto?> GetWorkItemByIdAsync(int id, string userId, bool isAdmin);
        Task<WorkItemDto> CreateWorkItemAsync(CreateWorkItemDto workItemDto, string createdById);
        Task<UpdateWorkItemDto?> UpdateWorkItemAsync(UpdateWorkItemDto workItemDto, string userId, bool isAdmin);
        Task<bool> DeleteWorkItemAsync(int id, string userId, bool isAdmin);
        Task<bool> UpdateWorkItemStatusAsync(int id, string status, string userId, bool isAdmin);
        Task<CommentDto> AddCommentAsync(int workItemId, string content, string authorId);
        Task<IEnumerable<CommentDto>> GetCommentsAsync(int workItemId);
        Task<bool> AssignWorkItemAsync(int id, string? assigneeId, string userId, bool isAdmin);
        Task<int> GetWorkItemsCountByStatusAsync(int projectId, string status);
        Task<IEnumerable<WorkItemDto>> GetWorkItemsByAssigneeAsync(string assigneeId, string userId, bool isAdmin);
        Task<PaginatedResult<WorkItemDto>> GetFilteredWorkItemsAsync(WorkItemFilterDto filter);
    }
}