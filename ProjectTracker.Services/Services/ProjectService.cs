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
                .Where(p => !p.IsDeleted);

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
                OwnerName = p.Owner.FullName,
                CreatedAt = p.CreatedAt,
                TeamMembersCount = p.TeamMembers.Count,
                WorkItemsCount = p.WorkItems.Count,
                CompletedWorkItemsCount = p.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                CompletionPercentage = p.WorkItems.Count > 0
                    ? (double)p.WorkItems.Count(w => w.Status == WorkItemStatus.Done) / p.WorkItems.Count * 100
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

            if (project == null)
            {
                return null;
            }

            // Check access
            var isOwner = project.OwnerId == userId;
            var isTeamMember = project.TeamMembers.Any(tm => tm.UserId == userId);

            if (!isAdmin && !isOwner && !isTeamMember)
            {
                return null;
            }

            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status.ToString(),
                OwnerId = project.OwnerId,
                OwnerName = project.Owner.FullName,
                CreatedAt = project.CreatedAt,
                TeamMembersCount = project.TeamMembers.Count,
                WorkItemsCount = project.WorkItems.Count,
                CompletedWorkItemsCount = project.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                CompletionPercentage = project.WorkItems.Count > 0
                    ? (double)project.WorkItems.Count(w => w.Status == WorkItemStatus.Done) / project.WorkItems.Count * 100
                    : 0
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
    }
}