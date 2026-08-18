using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Constants;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;

namespace ProjectTracker.Services.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<AdminStatisticsDto> GetStatisticsAsync()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var totalProjects = await _context.Projects.CountAsync(p => !p.IsDeleted);
            var totalWorkItems = await _context.WorkItems.CountAsync();
            var totalComments = await _context.Comments.CountAsync();

            // Optimized with Select instead of Include
            var recentProjects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new RecentProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    OwnerName = p.Owner != null ? p.Owner.FullName : "Unknown",
                    CreatedAt = p.CreatedAt,
                    Status = p.Status.ToString()
                }).ToListAsync();

            // Optimized with Select instead of loading entire users
            var recentUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new RecentUserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                }).ToListAsync();

            return new AdminStatisticsDto
            {
                TotalUsers = totalUsers,
                TotalProjects = totalProjects,
                TotalWorkItems = totalWorkItems,
                TotalComments = totalComments,
                ActiveUsers = activeUsers,
                RecentProjects = recentProjects,
                RecentUsers = recentUsers
            };
        }

        public async Task<PaginatedResult<UserAdminDto>> GetUsersAsync(string? searchTerm, int page, int pageSize, bool? isActive = null)
        {
            var query = _userManager.Users.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Email != null &&
                    (u.Email.Contains(searchTerm) || u.FullName.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            // Optimized with Select instead of loading entire users
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserAdminDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    Department = u.Department,
                    JobTitle = u.JobTitle,
                    Bio = u.Bio,
                    Roles = new List<string>() // Roles will be loaded separately
                })
                .ToListAsync();

            // Load roles for all users in one query
            var userIds = users.Select(u => u.Id).ToList();
            var userRoles = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .ToListAsync();

            foreach (var user in users)
            {
                user.Roles = userRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Select(ur => ur.RoleName ?? string.Empty)
                    .ToList();
            }

            return new PaginatedResult<UserAdminDto>
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<UserAdminDto?> GetUserByIdAsync(string userId)
        {
            // Optimized with Select
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserAdminDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    Department = u.Department,
                    JobTitle = u.JobTitle,
                    Bio = u.Bio
                })
                .FirstOrDefaultAsync();

            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(
                new ApplicationUser { Id = userId });

            user.Roles = roles.ToList();

            return user;
        }

        public async Task<bool> UpdateUserAsync(EditUserDto userDto)
        {
            var user = await _userManager.FindByIdAsync(userDto.Id);
            if (user == null) return false;

            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.Department = userDto.Department;
            user.JobTitle = userDto.JobTitle;
            user.Bio = userDto.Bio;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return false;

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = userDto.SelectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(userDto.SelectedRoles).ToList();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            var updatedRoles = await _userManager.GetRolesAsync(user);
            if (!updatedRoles.Any() && !rolesToAdd.Any())
            {
                await _userManager.AddToRoleAsync(user, RoleNames.Viewer);
            }

            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            return true;
        }

        public async Task<bool> DeleteUserAsync(string userId, string currentUserId)
        {
            if (userId == currentUserId) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<PaginatedResult<AdminProjectDto>> GetProjectsAsync(string? searchTerm, string? status, int page, int pageSize)
        {
            var query = _context.Projects
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                var statusEnum = Enum.Parse<ProjectStatus>(status);
                query = query.Where(p => p.Status == statusEnum);
            }

            var totalCount = await query.CountAsync();

            // Optimized with Select - includes WorkItems count
            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status.ToString(),
                    OwnerName = p.Owner != null ? p.Owner.FullName : "Unknown",
                    WorkItemsCount = p.WorkItems.Count,
                    CreatedAt = p.CreatedAt,
                    IsDeleted = p.IsDeleted
                })
                .ToListAsync();

            return new PaginatedResult<AdminProjectDto>
            {
                Items = projects,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> UpdateProjectStatusAsync(int projectId, string status)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return false;

            project.Status = Enum.Parse<ProjectStatus>(status);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> HardDeleteProjectAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.WorkItems)
                .Include(p => p.TeamMembers)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return false;

            _context.WorkItems.RemoveRange(project.WorkItems);
            _context.TeamMembers.RemoveRange(project.TeamMembers);
            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            var roles = await _roleManager.Roles
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    UserCount = 0 // Will be updated
                })
                .ToListAsync();

            // Get user counts for each role efficiently
            var roleCounts = await _context.UserRoles
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count);

            foreach (var role in roles)
            {
                if (roleCounts.TryGetValue(role.Id, out var count))
                {
                    role.UserCount = count;
                }
            }

            return roles;
        }

        public async Task<bool> CreateRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;

            if (await _roleManager.RoleExistsAsync(roleName)) return false;

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            return result.Succeeded;
        }

        public async Task<bool> DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null || string.IsNullOrEmpty(role.Name))
            {
                return false;
            }

            if (RoleNames.AllRoles.Contains(role.Name))
            {
                return false;
            }

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded;
        }

        public async Task<bool> UpdateLastLoginAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.LastLoginAt = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}