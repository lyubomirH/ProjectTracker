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

            return members.Select(m => new TeamMemberDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                UserId = m.UserId,
                UserName = m.User.FullName,
                UserEmail = m.User.Email ?? string.Empty,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt,
                IsActive = m.IsActive
            });
        }

        public async Task<TeamMemberDto> AddTeamMemberAsync(int projectId, string userId, string role, string addedByUserId)
        {
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

                return await GetTeamMemberDto(existing);
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

            return await GetTeamMemberDto(teamMember);
        }

        public async Task<bool> RemoveTeamMemberAsync(int projectId, string userId, string removedByUserId)
        {
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId);

            if (teamMember == null)
            {
                return false;
            }

            teamMember.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTeamMemberRoleAsync(int projectId, string userId, string newRole, string updatedByUserId)
        {
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);

            if (teamMember == null)
            {
                return false;
            }

            teamMember.Role = Enum.Parse<TeamRole>(newRole);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAvailableUsersForProjectAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.TeamMembers)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return new List<UserDto>();
            }

            var existingUserIds = project.TeamMembers
                .Where(tm => tm.IsActive)
                .Select(tm => tm.UserId)
                .ToList();

            var availableUsers = await _context.Users
                .Where(u => !existingUserIds.Contains(u.Id) && u.IsActive)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName,
                    Department = u.Department
                })
                .ToListAsync();

            return availableUsers;
        }

        public async Task<bool> IsUserTeamMemberAsync(int projectId, string userId)
        {
            return await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == projectId && tm.UserId == userId && tm.IsActive);
        }

        private async Task<TeamMemberDto> GetTeamMemberDto(TeamMember teamMember)
        {
            var user = await _context.Users.FindAsync(teamMember.UserId);
            var project = await _context.Projects.FindAsync(teamMember.ProjectId);

            return new TeamMemberDto
            {
                Id = teamMember.Id,
                ProjectId = teamMember.ProjectId,
                ProjectName = project?.Name ?? string.Empty,
                UserId = teamMember.UserId,
                UserName = user?.FullName ?? "Unknown",
                UserEmail = user?.Email ?? string.Empty,
                Role = teamMember.Role.ToString(),
                JoinedAt = teamMember.JoinedAt,
                IsActive = teamMember.IsActive
            };
        }
    }
}