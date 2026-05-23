using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;

namespace ProjectTracker.Services.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;

        public ProjectService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(string userId, bool isAdmin)
        {
            var query = _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.TeamMembers)
                .Include(p => p.WorkItems)
                .Where(p => !p.IsDeleted);  // Само веднъж

            if (!isAdmin)
            {
                query = query.Where(p =>
                    p.OwnerId == userId ||
                    p.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var projects = await query.ToListAsync();

            return projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status.ToString(),
                OwnerId = p.OwnerId,
                OwnerName = p.Owner?.FullName ?? "Unknown",
                CreatedAt = p.CreatedAt,
                TeamMembersCount = p.TeamMembers?.Count ?? 0,
                WorkItemsCount = p.WorkItems?.Count ?? 0,
                CompletedWorkItemsCount = p.WorkItems?.Count(w => w.Status == WorkItemStatus.Done) ?? 0,
                CompletionPercentage = (p.WorkItems?.Count ?? 0) > 0
                    ? (double)(p.WorkItems?.Count(w => w.Status == WorkItemStatus.Done) ?? 0) / (p.WorkItems?.Count ?? 0) * 100
                    : 0
            }).OrderByDescending(p => p.CreatedAt);
        }

        public async Task<ProjectDto?> GetProjectByIdAsync(int id, string userId, bool isAdmin)
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.TeamMembers)
                    .ThenInclude(tm => tm.User)
                .Include(p => p.WorkItems)
                    .ThenInclude(w => w.Assignee)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (project == null) return null;

            var isOwner = project.OwnerId == userId;
            var isTeamMember = project.TeamMembers?.Any(tm => tm.UserId == userId) ?? false;

            if (!isAdmin && !isOwner && !isTeamMember) return null;

            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status.ToString(),
                OwnerId = project.OwnerId,
                OwnerName = project.Owner?.FullName ?? "Unknown",
                CreatedAt = project.CreatedAt,
                TeamMembersCount = project.TeamMembers?.Count ?? 0,
                WorkItemsCount = project.WorkItems?.Count ?? 0,
                CompletedWorkItemsCount = project.WorkItems?.Count(w => w.Status == WorkItemStatus.Done) ?? 0,
                CompletionPercentage = (project.WorkItems?.Count ?? 0) > 0
                    ? (double)(project.WorkItems?.Count(w => w.Status == WorkItemStatus.Done) ?? 0) / (project.WorkItems?.Count ?? 0) * 100
                    : 0,
                TeamMembers = project.TeamMembers?.Select(tm => new TeamMemberDto
                {
                    UserId = tm.UserId,
                    UserName = tm.User?.FullName ?? "Unknown",
                    Role = tm.Role.ToString(),
                    JoinedAt = tm.JoinedAt
                }).ToList() ?? new List<TeamMemberDto>(),
                WorkItems = project.WorkItems?.Select(w => new WorkItemSummaryDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Status = w.Status.ToString(),
                    Priority = w.Priority.ToString(),
                    AssigneeName = w.Assignee?.FullName ?? "Unassigned",
                    DueDate = w.DueDate
                }).ToList() ?? new List<WorkItemSummaryDto>()
            };
        }

        public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto projectDto, string ownerId)
        {
            var project = new Project
            {
                Name = projectDto.Name,
                Description = projectDto.Description,
                StartDate = projectDto.StartDate,
                EndDate = projectDto.EndDate,
                Status = Enum.Parse<ProjectStatus>(projectDto.Status),
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Add owner as team member with ProjectManager role
            var teamMember = new TeamMember
            {
                ProjectId = project.Id,
                UserId = ownerId,
                Role = TeamRole.ProjectManager,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status.ToString(),
                OwnerId = project.OwnerId,
                OwnerName = project.Owner?.FullName ?? string.Empty,
                CreatedAt = project.CreatedAt,
                TeamMembersCount = 1,
                WorkItemsCount = 0,
                CompletedWorkItemsCount = 0,
                CompletionPercentage = 0
            };
        }

        public async Task<UpdateProjectDto?> UpdateProjectAsync(UpdateProjectDto projectDto, string userId, bool isAdmin)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectDto.Id && !p.IsDeleted);

            if (project == null)
            {
                return null;
            }

            // Check permission - only Admin or Project Owner can update
            if (!isAdmin && project.OwnerId != userId)
            {
                return null;
            }

            project.Name = projectDto.Name;
            project.Description = projectDto.Description;
            project.StartDate = projectDto.StartDate;
            project.EndDate = projectDto.EndDate;
            project.Status = Enum.Parse<ProjectStatus>(projectDto.Status);

            await _context.SaveChangesAsync();

            return projectDto;
        }

        public async Task<bool> DeleteProjectAsync(int id, string userId, bool isAdmin)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (project == null)
            {
                return false;
            }

            // Check permission - only Admin or Project Owner can delete
            if (!isAdmin && project.OwnerId != userId)
            {
                return false;
            }

            project.IsDeleted = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsUserInProjectAsync(int projectId, string userId)
        {
            return await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);
        }

        public async Task<int> GetProjectWorkItemsCountAsync(int projectId)
        {
            return await _context.WorkItems
                .CountAsync(w => w.ProjectId == projectId);
        }

        public async Task<double> GetProjectCompletionPercentageAsync(int projectId)
        {
            var total = await _context.WorkItems.CountAsync(w => w.ProjectId == projectId);
            if (total == 0) return 0;

            var completed = await _context.WorkItems
                .CountAsync(w => w.ProjectId == projectId && w.Status == WorkItemStatus.Done);

            return (double)completed / total * 100;
        }
        public async Task<PaginatedResult<ProjectDto>> GetFilteredProjectsAsync(ProjectFilterDto filter)
        {
            var query = _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.TeamMembers)
                .Include(p => p.WorkItems)
                .Where(p => !p.IsDeleted);

            if (!filter.IsAdmin)
            {
                query = query.Where(p =>
                    p.OwnerId == filter.UserId ||
                    p.TeamMembers.Any(tm => tm.UserId == filter.UserId));
            }

            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "All")
            {
                var status = Enum.Parse<ProjectStatus>(filter.Status);
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p => p.Name.Contains(filter.SearchTerm) ||
                                         (p.Description != null && p.Description.Contains(filter.SearchTerm)));
            }

            query = (filter.SortBy?.ToLower()) switch
            {
                "name" => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "startdate" => filter.SortDescending ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate),
                "status" => filter.SortDescending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
                "workitemscount" => filter.SortDescending ? query.OrderByDescending(p => p.WorkItems.Count) : query.OrderBy(p => p.WorkItems.Count),
                _ => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var projectDtos = items.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status.ToString(),
                OwnerId = p.OwnerId,
                OwnerName = p.Owner?.FullName ?? "Unknown",
                CreatedAt = p.CreatedAt,
                TeamMembersCount = p.TeamMembers?.Count ?? 0,
                WorkItemsCount = p.WorkItems?.Count ?? 0,
                CompletedWorkItemsCount = p.WorkItems?.Count(w => w.Status == WorkItemStatus.Done) ?? 0,
                CompletionPercentage = (p.WorkItems?.Count ?? 0) > 0
                    ? (double)(p.WorkItems?.Count(w => w.Status == WorkItemStatus.Done) ?? 0) / (p.WorkItems?.Count ?? 0) * 100
                    : 0
            });

            return new PaginatedResult<ProjectDto>
            {
                Items = projectDtos,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
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