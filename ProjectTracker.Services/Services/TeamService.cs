using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public TeamService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        

        public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int projectId)
        {
            var members = await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.ProjectId == projectId && tm.IsActive)
                .ToListAsync();

            var project = await _context.Projects.FindAsync(projectId);

            var result = new List<TeamMemberDto>();

            if (project?.Owner != null)
            {
                var ownerExists = members.Any(m => m.UserId == project.OwnerId);
                if (!ownerExists)
                {
                    result.Add(new TeamMemberDto
                    {
                        Id = 0,
                        ProjectId = projectId,
                        ProjectName = project.Name,
                        UserId = project.Owner.Id,
                        UserName = project.Owner.FullName,
                        UserEmail = project.Owner.Email ?? string.Empty,
                        Role = "Owner",
                        JoinedAt = project.CreatedAt,
                        IsActive = true
                    });
                }
            }

            foreach (var member in members)
            {
                result.Add(new TeamMemberDto
                {
                    Id = member.Id,
                    ProjectId = member.ProjectId,
                    ProjectName = project?.Name ?? string.Empty,
                    UserId = member.UserId,
                    UserName = member.User?.FullName ?? "Unknown",
                    UserEmail = member.User?.Email ?? string.Empty,
                    Role = member.Role.ToString(),
                    JoinedAt = member.JoinedAt,
                    IsActive = member.IsActive
                });
            }

            return result;
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
                UserName = member.User?.FullName ?? "Unknown",
                UserEmail = member.User?.Email ?? string.Empty,
                Role = member.Role.ToString(),
                JoinedAt = member.JoinedAt,
                IsActive = member.IsActive
            };
        }

        public async Task<TeamMemberDto> AddTeamMemberAsync(int projectId, string userId, string role, string addedByUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

            if (project == null)
            {
                throw new InvalidOperationException("Project not found");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

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

        public async Task<bool> CanUserManageTeamAsync(int projectId, string userId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

            if (project == null) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return true;

            if (project.OwnerId == userId) return true;

            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);

            if (teamMember == null) return false;

            return teamMember.Role == TeamRole.ProjectManager;
        }

        public async Task<IEnumerable<UserDto>> GetProjectManagersAsync()
        {
            var users = await _context.Users.ToListAsync();
            var projectManagers = new List<UserDto>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "ProjectManager"))
                {
                    projectManagers.Add(new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? string.Empty,
                        FullName = user.FullName,
                        Department = user.Department,
                        JobTitle = user.JobTitle,
                        AvatarUrl = user.AvatarUrl
                    });
                }
            }

            return projectManagers;
        }

        public async Task<IEnumerable<TeamMemberDto>> GetUserProjectsAsync(string userId)
        {
            var teamMembers = await _context.TeamMembers
                .Include(tm => tm.Project)
                .Where(tm => tm.UserId == userId && tm.IsActive)
                .ToListAsync();

            return teamMembers.Select(tm => new TeamMemberDto
            {
                Id = tm.Id,
                ProjectId = tm.ProjectId,
                ProjectName = tm.Project?.Name ?? string.Empty,
                UserId = tm.UserId,
                Role = tm.Role.ToString(),
                JoinedAt = tm.JoinedAt,
                IsActive = tm.IsActive
            });
        }

        public async Task<List<TeamMemberSimpleDto>> GetTeamMembersForDropdownAsync(int projectId)
        {
            var members = new List<TeamMemberSimpleDto>();

            var project = await _context.Projects
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

            if (project == null) return members;

            if (project.Owner != null)
            {
                members.Add(new TeamMemberSimpleDto
                {
                    UserId = project.Owner.Id,
                    UserName = project.Owner.FullName,
                    Role = "Owner"
                });
            }

            var teamMembers = await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.ProjectId == projectId && tm.IsActive)
                .ToListAsync();

            foreach (var tm in teamMembers)
            {
                if (tm.UserId == project.OwnerId) continue;

                if (tm.User != null)
                {
                    members.Add(new TeamMemberSimpleDto
                    {
                        UserId = tm.UserId,
                        UserName = tm.User.FullName,
                        Role = tm.Role.ToString()
                    });
                }
            }

            return members;
        }
    }
}