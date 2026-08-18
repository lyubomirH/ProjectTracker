using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public TeamService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IEnumerable<TeamMemberDto>> GetTeamMembersAsync(int projectId)
        {
            var project = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => new { p.Name, p.OwnerId, p.Owner })
                .FirstOrDefaultAsync();

            if (project == null) return new List<TeamMemberDto>();

            var members = await _context.TeamMembers
                .Where(tm => tm.ProjectId == projectId && tm.IsActive)
                .Select(tm => new TeamMemberDto
                {
                    Id = tm.Id,
                    ProjectId = tm.ProjectId,
                    ProjectName = tm.Project != null ? tm.Project.Name : string.Empty,
                    UserId = tm.UserId,
                    UserName = tm.User != null ? tm.User.FullName : "Unknown",
                    UserEmail = tm.User != null ? tm.User.Email ?? string.Empty : string.Empty,
                    Role = tm.Role.ToString(),
                    JoinedAt = tm.JoinedAt,
                    IsActive = tm.IsActive
                })
                .ToListAsync();

            // Add owner if not already in the list
            if (project.Owner != null && !members.Any(m => m.UserId == project.OwnerId))
            {
                members.Insert(0, new TeamMemberDto
                {
                    Id = 0,
                    ProjectId = projectId,
                    ProjectName = project.Name,
                    UserId = project.Owner.Id,
                    UserName = project.Owner.FullName,
                    UserEmail = project.Owner.Email ?? string.Empty,
                    Role = "Owner",
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }

            return members;
        }

        public async Task<TeamMemberDto?> GetTeamMemberAsync(int projectId, string userId)
        {
            var member = await _context.TeamMembers
                .Where(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive)
                .Select(tm => new TeamMemberDto
                {
                    Id = tm.Id,
                    ProjectId = tm.ProjectId,
                    ProjectName = tm.Project != null ? tm.Project.Name : string.Empty,
                    UserId = tm.UserId,
                    UserName = tm.User != null ? tm.User.FullName : "Unknown",
                    UserEmail = tm.User != null ? tm.User.Email ?? string.Empty : string.Empty,
                    Role = tm.Role.ToString(),
                    JoinedAt = tm.JoinedAt,
                    IsActive = tm.IsActive
                })
                .FirstOrDefaultAsync();

            return member;
        }

        public async Task<TeamMemberDto> AddTeamMemberAsync(int projectId, string userId, string role, string addedByUserId)
        {
            var project = await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Select(p => new { p.Name, p.OwnerId })
                .FirstOrDefaultAsync();

            if (project == null)
            {
                throw new InvalidOperationException("Project not found");
            }

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.FullName, u.Email })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Check permissions
            if (project.OwnerId != addedByUserId)
            {
                var addingUser = await _context.TeamMembers
                    .Where(tm => tm.ProjectId == projectId && tm.UserId == addedByUserId && tm.IsActive)
                    .Select(tm => tm.Role)
                    .FirstOrDefaultAsync();

                if (addingUser != TeamRole.ProjectManager)
                {
                    throw new UnauthorizedAccessException("You don't have permission to add team members");
                }
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

            var project = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => new { p.OwnerId })
                .FirstOrDefaultAsync();

            if (project == null) return false;

            if (project.OwnerId == userId) return false;

            // Check permissions
            if (project.OwnerId != removedByUserId)
            {
                var removingUser = await _context.TeamMembers
                    .Where(tm => tm.ProjectId == projectId && tm.UserId == removedByUserId && tm.IsActive)
                    .Select(tm => tm.Role)
                    .FirstOrDefaultAsync();

                if (removingUser != TeamRole.ProjectManager)
                {
                    return false;
                }
            }

            teamMember.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTeamMemberRoleAsync(int projectId, string userId, string newRole, string updatedByUserId)
        {
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);

            if (teamMember == null) return false;

            var project = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => new { p.OwnerId })
                .FirstOrDefaultAsync();

            if (project == null) return false;

            // Check permissions
            if (project.OwnerId != updatedByUserId)
            {
                var updatingUser = await _context.TeamMembers
                    .Where(tm => tm.ProjectId == projectId && tm.UserId == updatedByUserId && tm.IsActive)
                    .Select(tm => tm.Role)
                    .FirstOrDefaultAsync();

                if (updatingUser != TeamRole.ProjectManager)
                {
                    return false;
                }
            }

            teamMember.Role = Enum.Parse<TeamRole>(newRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAvailableUsersForProjectAsync(int projectId)
        {
            var project = await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Select(p => new
                {
                    ExistingUserIds = p.TeamMembers
                        .Where(tm => tm.IsActive)
                        .Select(tm => tm.UserId)
                        .ToList(),
                    OwnerId = p.OwnerId
                })
                .FirstOrDefaultAsync();

            if (project == null) return new List<UserDto>();

            var existingUserIds = project.ExistingUserIds.ToList();
            if (!existingUserIds.Contains(project.OwnerId))
            {
                existingUserIds.Add(project.OwnerId);
            }

            // Optimized with Select
            var availableUsers = await _context.Users
                .Where(u => !existingUserIds.Contains(u.Id))
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
            return await _context.TeamMembers
                .Where(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive)
                .Select(tm => tm.Role == TeamRole.ProjectManager)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CanUserManageTeamAsync(int projectId, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return true;

            var project = await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Select(p => new { p.OwnerId })
                .FirstOrDefaultAsync();

            if (project == null) return false;

            if (project.OwnerId == userId) return true;

            var teamMember = await _context.TeamMembers
                .Where(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive)
                .Select(tm => tm.Role)
                .FirstOrDefaultAsync();

            return teamMember == TeamRole.ProjectManager;
        }

        public async Task<IEnumerable<UserDto>> GetProjectManagersAsync()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync("ProjectManager");

            return usersInRole.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                Department = u.Department,
                JobTitle = u.JobTitle,
                AvatarUrl = u.AvatarUrl
            });
        }

        public async Task<IEnumerable<TeamMemberDto>> GetUserProjectsAsync(string userId)
        {
            // Optimized with Select
            return await _context.TeamMembers
                .Where(tm => tm.UserId == userId && tm.IsActive)
                .Select(tm => new TeamMemberDto
                {
                    Id = tm.Id,
                    ProjectId = tm.ProjectId,
                    ProjectName = tm.Project != null ? tm.Project.Name : string.Empty,
                    UserId = tm.UserId,
                    Role = tm.Role.ToString(),
                    JoinedAt = tm.JoinedAt,
                    IsActive = tm.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<TeamMemberSimpleDto>> GetTeamMembersForDropdownAsync(int projectId)
        {
            var result = new List<TeamMemberSimpleDto>();

            var project = await _context.Projects
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Select(p => new
                {
                    OwnerId = p.OwnerId,
                    OwnerName = p.Owner != null ? p.Owner.FullName : "Unknown"
                })
                .FirstOrDefaultAsync();

            if (project == null) return result;

            result.Add(new TeamMemberSimpleDto
            {
                UserId = project.OwnerId,
                UserName = project.OwnerName,
                Role = "Owner"
            });

            // Optimized with Select
            var teamMembers = await _context.TeamMembers
                .Where(tm => tm.ProjectId == projectId && tm.IsActive && tm.UserId != project.OwnerId)
                .Select(tm => new TeamMemberSimpleDto
                {
                    UserId = tm.UserId,
                    UserName = tm.User != null ? tm.User.FullName : "Unknown",
                    Role = tm.Role.ToString()
                })
                .ToListAsync();

            result.AddRange(teamMembers);

            return result;
        }
    }
}