using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;

namespace ProjectTracker.Services.Services
{
    public class TeamService : ITeamService
    {
        private readonly ApplicationDbContext _context;

        public TeamService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int projectId)
        {
            var members = await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.ProjectId == projectId && tm.IsActive)
                .ToListAsync();

            var project = await _context.Projects.FindAsync(projectId);

            return members.Select(m => new TeamMemberDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                ProjectName = project?.Name ?? string.Empty,
                UserId = m.UserId,
                UserName = m.User.FullName,
                UserEmail = m.User.Email ?? string.Empty,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt,
                IsActive = m.IsActive
            });
        }

        public async Task<TeamMemberDto?> GetTeamMemberAsync(int projectId, string userId)
        {
            var member = await _context.TeamMembers
                .Include(tm => tm.User)
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);

            if (member == null) return null;

            var project = await _context.Projects.FindAsync(projectId);

            return new TeamMemberDto
            {
                Id = member.Id,
                ProjectId = member.ProjectId,
                ProjectName = project?.Name ?? string.Empty,
                UserId = member.UserId,
                UserName = member.User.FullName,
                UserEmail = member.User.Email ?? string.Empty,
                Role = member.Role.ToString(),
                JoinedAt = member.JoinedAt,
                IsActive = member.IsActive
            };
        }

        public async Task<TeamMemberDto> AddTeamMemberAsync(int projectId, string userId, string role, string addedByUserId)
        {
            // Check if project exists
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

            if (project == null)
            {
                throw new InvalidOperationException("Project not found");
            }

            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Check if already a member
            var existing = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId);

            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    existing.Role = Enum.Parse<TeamRole>(role);
                    existing.JoinedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return new TeamMemberDto
                {
                    Id = existing.Id,
                    ProjectId = existing.ProjectId,
                    ProjectName = project.Name,
                    UserId = existing.UserId,
                    UserName = user.FullName,
                    UserEmail = user.Email ?? string.Empty,
                    Role = existing.Role.ToString(),
                    JoinedAt = existing.JoinedAt,
                    IsActive = existing.IsActive
                };
            }

            var teamMember = new TeamMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = Enum.Parse<TeamRole>(role),
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            return new TeamMemberDto
            {
                Id = teamMember.Id,
                ProjectId = teamMember.ProjectId,
                ProjectName = project.Name,
                UserId = teamMember.UserId,
                UserName = user.FullName,
                UserEmail = user.Email ?? string.Empty,
                Role = teamMember.Role.ToString(),
                JoinedAt = teamMember.JoinedAt,
                IsActive = teamMember.IsActive
            };
        }

        public async Task<bool> RemoveTeamMemberAsync(int projectId, string userId, string removedByUserId)
        {
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId);

            if (teamMember == null) return false;

            // Don't allow removing the project owner
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null && project.OwnerId == userId) return false;

            teamMember.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTeamMemberRoleAsync(int projectId, string userId, string newRole, string updatedByUserId)
        {
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);

            if (teamMember == null) return false;

            teamMember.Role = Enum.Parse<TeamRole>(newRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAvailableUsersForProjectAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.TeamMembers)
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

            if (project == null) return new List<UserDto>();

            var existingUserIds = project.TeamMembers
                .Where(tm => tm.IsActive)
                .Select(tm => tm.UserId)
                .ToList();

            // Also exclude the project owner
            if (!existingUserIds.Contains(project.OwnerId))
            {
                existingUserIds.Add(project.OwnerId);
            }

            var availableUsers = await _context.Users
                .Where(u => !existingUserIds.Contains(u.Id) && u.IsActive)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName,
                    Department = u.Department,
                    JobTitle = u.JobTitle,
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync();

            return availableUsers;
        }

        public async Task<bool> IsUserTeamMemberAsync(int projectId, string userId)
        {
            return await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);
        }

        public async Task<bool> IsUserProjectManagerAsync(int projectId, string userId)
        {
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);

            if (teamMember == null) return false;

            return teamMember.Role == TeamRole.ProjectManager;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName,
                    Department = u.Department,
                    JobTitle = u.JobTitle,
                    AvatarUrl = u.AvatarUrl
                })
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return users;
        }

        public async Task<Dictionary<string, int>> GetTeamStatisticsAsync(string userId, bool isAdmin)
        {
            var stats = new Dictionary<string, int>();

            var projectsQuery = _context.Projects.Where(p => !p.IsDeleted);

            if (!isAdmin)
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.OwnerId == userId ||
                    p.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var projects = await projectsQuery.ToListAsync();
            stats["TotalProjects"] = projects.Count;
            stats["ActiveProjects"] = projects.Count(p => p.Status == ProjectStatus.Active);
            stats["CompletedProjects"] = projects.Count(p => p.Status == ProjectStatus.Completed);

            var workItemsQuery = _context.WorkItems.Where(w => !w.Project.IsDeleted);

            if (!isAdmin)
            {
                workItemsQuery = workItemsQuery.Where(w =>
                    w.AssigneeId == userId ||
                    w.CreatedById == userId ||
                    w.Project.OwnerId == userId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var workItems = await workItemsQuery.ToListAsync();
            stats["TotalWorkItems"] = workItems.Count;
            stats["CompletedWorkItems"] = workItems.Count(w => w.Status == WorkItemStatus.Done);
            stats["InProgressWorkItems"] = workItems.Count(w => w.Status == WorkItemStatus.InProgress);
            stats["ToDoWorkItems"] = workItems.Count(w => w.Status == WorkItemStatus.ToDo);
            stats["BlockedWorkItems"] = workItems.Count(w => w.Status == WorkItemStatus.Blocked);

            return stats;
        }
    }
}