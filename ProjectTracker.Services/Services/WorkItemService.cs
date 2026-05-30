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
                .Include(w => w.Project)
                .Include(w => w.Assignee)
                .Include(w => w.CreatedBy)
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

            var workItems = await query.ToListAsync();

            return workItems.Select(w => new WorkItemDto
            {
                Id = w.Id,
                Title = w.Title,
                Description = w.Description,
                Priority = w.Priority.ToString(),
                Status = w.Status.ToString(),
                ProjectId = w.ProjectId,
                ProjectName = w.Project.Name,
                AssigneeId = w.AssigneeId,
                AssigneeName = w.Assignee?.FullName ?? "Unassigned",
                CreatedById = w.CreatedById ?? string.Empty,
                CreatedByName = w.CreatedBy?.FullName ?? "Unknown",
                CreatedAt = w.CreatedAt,
                DueDate = w.DueDate,
                CompletedAt = w.CompletedAt,
                EstimatedHours = w.EstimatedHours,
                ActualHours = w.ActualHours,
                CommentsCount = w.Comments.Count
            }).OrderByDescending(w => w.CreatedAt);
        }

        public async Task<WorkItemDto?> GetWorkItemByIdAsync(int id, string userId, bool isAdmin)
        {
            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .Include(w => w.Assignee)
                .Include(w => w.CreatedBy)
                .Include(w => w.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null)
            {
                return null;
            }

            // Check access
            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId && tm.IsActive);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId && workItem.CreatedById != userId)
            {
                return null;
            }

            return new WorkItemDto
            {
                Id = workItem.Id,
                Title = workItem.Title,
                Description = workItem.Description,
                Priority = workItem.Priority.ToString(),
                Status = workItem.Status.ToString(),
                ProjectId = workItem.ProjectId,
                ProjectName = workItem.Project.Name,
                AssigneeId = workItem.AssigneeId,
                AssigneeName = workItem.Assignee?.FullName ?? "Unassigned",
                CreatedById = workItem.CreatedById ?? string.Empty,
                CreatedByName = workItem.CreatedBy?.FullName ?? "Unknown",
                CreatedAt = workItem.CreatedAt,
                DueDate = workItem.DueDate,
                CompletedAt = workItem.CompletedAt,
                EstimatedHours = workItem.EstimatedHours,
                ActualHours = workItem.ActualHours,
                CommentsCount = workItem.Comments.Count
            };
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

            var project = await _context.Projects.FindAsync(workItemDto.ProjectId);

            return new WorkItemDto
            {
                Id = workItem.Id,
                Title = workItem.Title,
                Description = workItem.Description,
                Priority = workItem.Priority.ToString(),
                Status = workItem.Status.ToString(),
                ProjectId = workItem.ProjectId,
                ProjectName = project?.Name ?? string.Empty,
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

            if (workItem == null)
            {
                return null;
            }

            // Check permission
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

            // Update completion date if status changed to/from Done
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

            if (workItem == null)
            {
                return false;
            }

            // Check permission - only Admin or Project Owner can delete
            var isOwner = workItem.Project.OwnerId == userId;

            if (!isAdmin && !isOwner)
            {
                return false;
            }

            _context.WorkItems.Remove(workItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateWorkItemStatusAsync(int id, string status, string userId, bool isAdmin)
        {
            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null)
            {
                return false;
            }

            // Check permission
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

            // Update completion date if status changed to/from Done
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

            var author = await _context.Users.FindAsync(authorId);

            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                WorkItemId = comment.WorkItemId,
                AuthorId = comment.AuthorId,
                AuthorName = author?.FullName ?? "Unknown",
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<IEnumerable<CommentDto>> GetCommentsAsync(int workItemId)
        {
            var comments = await _context.Comments
                .Include(c => c.Author)
                .Where(c => c.WorkItemId == workItemId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                WorkItemId = c.WorkItemId,
                AuthorId = c.AuthorId,
                AuthorName = c.Author.FullName,
                CreatedAt = c.CreatedAt
            });
        }

        public async Task<bool> AssignWorkItemAsync(int id, string assigneeId, string userId, bool isAdmin)
        {
            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null)
            {
                return false;
            }

            // Check permission
            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId && tm.IsActive);

            if (!isAdmin && !isOwner && !isTeamMember)
            {
                return false;
            }

            // Verify assignee is a team member
            var isAssigneeValid = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == assigneeId && tm.IsActive);

            if (!isAssigneeValid && !string.IsNullOrEmpty(assigneeId))
            {
                return false;
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
                .Include(w => w.Project)
                .Include(w => w.Assignee)
                .Include(w => w.CreatedBy)
                .Where(w => !w.Project.IsDeleted && w.AssigneeId == assigneeId);

            if (!isAdmin)
            {
                query = query.Where(w =>
                    w.CreatedById == userId ||
                    w.Project.OwnerId == userId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var workItems = await query.ToListAsync();

            return workItems.Select(w => new WorkItemDto
            {
                Id = w.Id,
                Title = w.Title,
                Description = w.Description,
                Priority = w.Priority.ToString(),
                Status = w.Status.ToString(),
                ProjectId = w.ProjectId,
                ProjectName = w.Project.Name,
                AssigneeId = w.AssigneeId,
                AssigneeName = w.Assignee?.FullName ?? "Unassigned",
                CreatedById = w.CreatedById ?? string.Empty,
                CreatedByName = w.CreatedBy?.FullName ?? "Unknown",
                CreatedAt = w.CreatedAt,
                DueDate = w.DueDate,
                CompletedAt = w.CompletedAt,
                EstimatedHours = w.EstimatedHours,
                ActualHours = w.ActualHours,
                CommentsCount = w.Comments.Count
            }).OrderByDescending(w => w.CreatedAt);
        }

        public async Task<PaginatedResult<WorkItemDto>> GetFilteredWorkItemsAsync(WorkItemFilterDto filter)
        {
            var query = _context.WorkItems
                .Include(w => w.Project)
                .Include(w => w.Assignee)
                .Include(w => w.CreatedBy)
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

            // Сортиране
            query = (filter.SortBy?.ToLower()) switch
            {
                "title" => filter.SortDescending ? query.OrderByDescending(w => w.Title) : query.OrderBy(w => w.Title),
                "duedate" => filter.SortDescending ? query.OrderByDescending(w => w.DueDate) : query.OrderBy(w => w.DueDate),
                "priority" => filter.SortDescending ? query.OrderByDescending(w => w.Priority) : query.OrderBy(w => w.Priority),
                "status" => filter.SortDescending ? query.OrderByDescending(w => w.Status) : query.OrderBy(w => w.Status),
                _ => filter.SortDescending ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var workItemDtos = items.Select(w => new WorkItemDto
            {
                Id = w.Id,
                Title = w.Title,
                Description = w.Description,
                Priority = w.Priority.ToString(),
                Status = w.Status.ToString(),
                ProjectId = w.ProjectId,
                ProjectName = w.Project.Name,
                AssigneeId = w.AssigneeId,
                AssigneeName = w.Assignee?.FullName ?? "Unassigned",
                CreatedById = w.CreatedById ?? string.Empty,
                CreatedByName = w.CreatedBy?.FullName ?? "Unknown",
                CreatedAt = w.CreatedAt,
                DueDate = w.DueDate,
                CompletedAt = w.CompletedAt,
                EstimatedHours = w.EstimatedHours,
                ActualHours = w.ActualHours,
                CommentsCount = w.Comments.Count
            });

            return new PaginatedResult<WorkItemDto>
            {
                Items = workItemDtos,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

    }
}