using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;

namespace ProjectTracker.Services.Services
{
    public class WorkItemService : IWorkItemService
    {
        private readonly ApplicationDbContext _context;

        public WorkItemService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WorkItemDto>> GetWorkItemsAsync(int? projectId, string userId, bool isAdmin)
        {
            var query = _context.WorkItems
                .Where(w => !w.Project.IsDeleted);

            if (projectId.HasValue)
            {
                query = query.Where(w => w.ProjectId == projectId.Value);
            }

            if (!isAdmin)
            {
                query = query.Where(w =>
                    w.AssigneeId == userId ||
                    w.CreatedById == userId ||
                    w.Project.OwnerId == userId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == userId));
            }

            // Optimized with Select
            return await query
                .Select(w => new WorkItemDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Description = w.Description,
                    Priority = w.Priority.ToString(),
                    Status = w.Status.ToString(),
                    ProjectId = w.ProjectId,
                    ProjectName = w.Project.Name,
                    AssigneeId = w.AssigneeId,
                    AssigneeName = w.Assignee != null ? w.Assignee.FullName : "Unassigned",
                    CreatedById = w.CreatedById ?? string.Empty,
                    CreatedByName = w.CreatedBy != null ? w.CreatedBy.FullName : "Unknown",
                    CreatedAt = w.CreatedAt,
                    DueDate = w.DueDate,
                    CompletedAt = w.CompletedAt,
                    EstimatedHours = w.EstimatedHours,
                    ActualHours = w.ActualHours,
                    CommentsCount = w.Comments.Count
                })
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<WorkItemDto?> GetWorkItemByIdAsync(int id, string userId, bool isAdmin)
        {
            var workItem = await _context.WorkItems
                .Where(w => w.Id == id && !w.Project.IsDeleted)
                .Select(w => new WorkItemDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Description = w.Description,
                    Priority = w.Priority.ToString(),
                    Status = w.Status.ToString(),
                    ProjectId = w.ProjectId,
                    ProjectName = w.Project.Name,
                    AssigneeId = w.AssigneeId,
                    AssigneeName = w.Assignee != null ? w.Assignee.FullName : "Unassigned",
                    CreatedById = w.CreatedById ?? string.Empty,
                    CreatedByName = w.CreatedBy != null ? w.CreatedBy.FullName : "Unknown",
                    CreatedAt = w.CreatedAt,
                    DueDate = w.DueDate,
                    CompletedAt = w.CompletedAt,
                    EstimatedHours = w.EstimatedHours,
                    ActualHours = w.ActualHours,
                    CommentsCount = w.Comments.Count
                })
                .FirstOrDefaultAsync();

            if (workItem == null) return null;

            // Check permissions
            var isOwner = await _context.Projects
                .Where(p => p.Id == workItem.ProjectId)
                .Select(p => p.OwnerId == userId)
                .FirstOrDefaultAsync();

            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId && tm.IsActive);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId && workItem.CreatedById != userId)
            {
                return null;
            }

            return workItem;
        }

        public async Task<WorkItemDto> CreateWorkItemAsync(CreateWorkItemDto workItemDto, string createdById)
        {
            var workItem = new WorkItem
            {
                Title = workItemDto.Title,
                Description = workItemDto.Description,
                Priority = Enum.Parse<WorkItemPriority>(workItemDto.Priority),
                Status = Enum.Parse<WorkItemStatus>(workItemDto.Status),
                ProjectId = workItemDto.ProjectId,
                AssigneeId = workItemDto.AssigneeId,
                CreatedById = createdById,
                DueDate = workItemDto.DueDate,
                EstimatedHours = workItemDto.EstimatedHours,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkItems.Add(workItem);
            await _context.SaveChangesAsync();

            var projectName = await _context.Projects
                .Where(p => p.Id == workItemDto.ProjectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();

            return new WorkItemDto
            {
                Id = workItem.Id,
                Title = workItem.Title,
                Description = workItem.Description,
                Priority = workItem.Priority.ToString(),
                Status = workItem.Status.ToString(),
                ProjectId = workItem.ProjectId,
                ProjectName = projectName ?? string.Empty,
                AssigneeId = workItem.AssigneeId,
                CreatedById = createdById,
                CreatedAt = workItem.CreatedAt,
                DueDate = workItem.DueDate,
                EstimatedHours = workItem.EstimatedHours,
                CommentsCount = 0
            };
        }

        public async Task<UpdateWorkItemDto?> UpdateWorkItemAsync(UpdateWorkItemDto workItemDto, string userId, bool isAdmin)
        {
            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == workItemDto.Id && !w.Project.IsDeleted);

            if (workItem == null) return null;

            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId && tm.IsActive);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId)
            {
                return null;
            }

            var oldStatus = workItem.Status;
            var newStatus = Enum.Parse<WorkItemStatus>(workItemDto.Status);

            workItem.Title = workItemDto.Title;
            workItem.Description = workItemDto.Description;
            workItem.Priority = Enum.Parse<WorkItemPriority>(workItemDto.Priority);
            workItem.Status = newStatus;
            workItem.AssigneeId = workItemDto.AssigneeId;
            workItem.DueDate = workItemDto.DueDate;
            workItem.EstimatedHours = workItemDto.EstimatedHours;
            workItem.ActualHours = workItemDto.ActualHours;

            if (oldStatus != WorkItemStatus.Done && newStatus == WorkItemStatus.Done)
            {
                workItem.CompletedAt = DateTime.UtcNow;
            }
            else if (newStatus != WorkItemStatus.Done)
            {
                workItem.CompletedAt = null;
            }

            await _context.SaveChangesAsync();

            return workItemDto;
        }

        public async Task<bool> DeleteWorkItemAsync(int id, string userId, bool isAdmin)
        {
            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null) return false;

            var isOwner = workItem.Project.OwnerId == userId;

            if (!isAdmin && !isOwner) return false;

            _context.WorkItems.Remove(workItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateWorkItemStatusAsync(int id, string status, string userId, bool isAdmin)
        {
            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null) return false;

            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId && tm.IsActive);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId)
            {
                return false;
            }

            var oldStatus = workItem.Status;
            var newStatus = Enum.Parse<WorkItemStatus>(status);

            workItem.Status = newStatus;

            if (oldStatus != WorkItemStatus.Done && newStatus == WorkItemStatus.Done)
            {
                workItem.CompletedAt = DateTime.UtcNow;
            }
            else if (newStatus != WorkItemStatus.Done)
            {
                workItem.CompletedAt = null;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<CommentDto> AddCommentAsync(int workItemId, string content, string authorId)
        {
            var comment = new Comment
            {
                Content = content,
                WorkItemId = workItemId,
                AuthorId = authorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var authorName = await _context.Users
                .Where(u => u.Id == authorId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                WorkItemId = comment.WorkItemId,
                AuthorId = comment.AuthorId,
                AuthorName = authorName ?? "Unknown",
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<IEnumerable<CommentDto>> GetCommentsAsync(int workItemId)
        {
            // Optimized with Select
            return await _context.Comments
                .Where(c => c.WorkItemId == workItemId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    WorkItemId = c.WorkItemId,
                    AuthorId = c.AuthorId,
                    AuthorName = c.Author != null ? c.Author.FullName : "Unknown",
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> AssignWorkItemAsync(int id, string assigneeId, string userId, bool isAdmin)
        {
            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null) return false;

            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId && tm.IsActive);

            if (!isAdmin && !isOwner && !isTeamMember) return false;

            if (!string.IsNullOrEmpty(assigneeId))
            {
                var isAssigneeValid = await _context.TeamMembers
                    .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == assigneeId && tm.IsActive);

                if (!isAssigneeValid) return false;
            }

            workItem.AssigneeId = string.IsNullOrEmpty(assigneeId) ? null : assigneeId;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> GetWorkItemsCountByStatusAsync(int projectId, string status)
        {
            var statusEnum = Enum.Parse<WorkItemStatus>(status);

            return await _context.WorkItems
                .CountAsync(w => w.ProjectId == projectId && w.Status == statusEnum);
        }

        public async Task<IEnumerable<WorkItemDto>> GetWorkItemsByAssigneeAsync(string assigneeId, string userId, bool isAdmin)
        {
            var query = _context.WorkItems
                .Where(w => !w.Project.IsDeleted && w.AssigneeId == assigneeId);

            if (!isAdmin)
            {
                query = query.Where(w =>
                    w.CreatedById == userId ||
                    w.Project.OwnerId == userId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == userId));
            }

            // Optimized with Select
            return await query
                .Select(w => new WorkItemDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Description = w.Description,
                    Priority = w.Priority.ToString(),
                    Status = w.Status.ToString(),
                    ProjectId = w.ProjectId,
                    ProjectName = w.Project.Name,
                    AssigneeId = w.AssigneeId,
                    AssigneeName = w.Assignee != null ? w.Assignee.FullName : "Unassigned",
                    CreatedById = w.CreatedById ?? string.Empty,
                    CreatedByName = w.CreatedBy != null ? w.CreatedBy.FullName : "Unknown",
                    CreatedAt = w.CreatedAt,
                    DueDate = w.DueDate,
                    CompletedAt = w.CompletedAt,
                    EstimatedHours = w.EstimatedHours,
                    ActualHours = w.ActualHours,
                    CommentsCount = w.Comments.Count
                })
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<PaginatedResult<WorkItemDto>> GetFilteredWorkItemsAsync(WorkItemFilterDto filter)
        {
            var query = _context.WorkItems
                .Where(w => !w.Project.IsDeleted);

            if (!filter.IsAdmin)
            {
                query = query.Where(w =>
                    w.AssigneeId == filter.UserId ||
                    w.CreatedById == filter.UserId ||
                    w.Project.OwnerId == filter.UserId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == filter.UserId));
            }

            if (filter.ProjectId.HasValue && filter.ProjectId.Value > 0)
            {
                query = query.Where(w => w.ProjectId == filter.ProjectId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "All")
            {
                var status = Enum.Parse<WorkItemStatus>(filter.Status);
                query = query.Where(w => w.Status == status);
            }

            if (!string.IsNullOrEmpty(filter.Priority) && filter.Priority != "All")
            {
                var priority = Enum.Parse<WorkItemPriority>(filter.Priority);
                query = query.Where(w => w.Priority == priority);
            }

            if (!string.IsNullOrEmpty(filter.AssigneeId))
            {
                query = query.Where(w => w.AssigneeId == filter.AssigneeId);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(w => w.Title.Contains(filter.SearchTerm) ||
                                         (w.Description != null && w.Description.Contains(filter.SearchTerm)));
            }

            query = (filter.SortBy?.ToLower()) switch
            {
                "title" => filter.SortDescending ? query.OrderByDescending(w => w.Title) : query.OrderBy(w => w.Title),
                "duedate" => filter.SortDescending ? query.OrderByDescending(w => w.DueDate) : query.OrderBy(w => w.DueDate),
                "priority" => filter.SortDescending ? query.OrderByDescending(w => w.Priority) : query.OrderBy(w => w.Priority),
                "status" => filter.SortDescending ? query.OrderByDescending(w => w.Status) : query.OrderBy(w => w.Status),
                _ => filter.SortDescending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            // Optimized with Select
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(w => new WorkItemDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Description = w.Description,
                    Priority = w.Priority.ToString(),
                    Status = w.Status.ToString(),
                    ProjectId = w.ProjectId,
                    ProjectName = w.Project.Name,
                    AssigneeId = w.AssigneeId,
                    AssigneeName = w.Assignee != null ? w.Assignee.FullName : "Unassigned",
                    CreatedById = w.CreatedById ?? string.Empty,
                    CreatedByName = w.CreatedBy != null ? w.CreatedBy.FullName : "Unknown",
                    CreatedAt = w.CreatedAt,
                    DueDate = w.DueDate,
                    CompletedAt = w.CompletedAt,
                    EstimatedHours = w.EstimatedHours,
                    ActualHours = w.ActualHours,
                    CommentsCount = w.Comments.Count
                })
                .ToListAsync();

            return new PaginatedResult<WorkItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
    }
}