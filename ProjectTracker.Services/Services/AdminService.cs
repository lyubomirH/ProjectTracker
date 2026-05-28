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

            var recentProjects = await _context.Projects
                .Include(p => p.Owner)
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

            var recentUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new RecentUserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive
                }).ToListAsync();

            return new AdminStatisticsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalProjects = totalProjects,
                TotalWorkItems = totalWorkItems,
                TotalComments = totalComments,
                RecentProjects = recentProjects,
                RecentUsers = recentUsers
            };
        }

        public async Task<PaginatedResult<UserAdminDto>> GetUsersAsync(string? searchTerm, int page, int pageSize)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Email != null && (u.Email.Contains(searchTerm) ||
                                         u.FullName.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserAdminDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserAdminDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive,
                    Roles = roles.ToList(),
                    Department = user.Department,
                    JobTitle = user.JobTitle,
                    Bio = user.Bio
                });
            }

            return new PaginatedResult<UserAdminDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<UserAdminDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserAdminDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                Department = user.Department,
                JobTitle = user.JobTitle,
                Bio = user.Bio
            };
        }

        public async Task<bool> UpdateUserAsync(EditUserDto userDto)
        {
            var user = await _userManager.FindByIdAsync(userDto.Id);
            if (user == null) return false;

            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.IsActive = userDto.IsActive;
            user.Department = userDto.Department;
            user.JobTitle = userDto.JobTitle;
            user.Bio = userDto.Bio;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return false;

            // Update roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = userDto.SelectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(userDto.SelectedRoles).ToList();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
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
                .Include(p => p.Owner)
                .Include(p => p.WorkItems)
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
            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var projectDtos = projects.Select(p => new AdminProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status.ToString(),
                OwnerName = p.Owner?.FullName ?? "Unknown",
                WorkItemsCount = p.WorkItems.Count,
                CreatedAt = p.CreatedAt,
                IsDeleted = p.IsDeleted
            }).ToList();

            return new PaginatedResult<AdminProjectDto>
            {
                Items = projectDtos,
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
            var roles = await _roleManager.Roles.ToListAsync();
            var roleDtos = new List<RoleDto>();

            foreach (var role in roles)
            {
                var userCount = await _userManager.GetUsersInRoleAsync(role.Name ?? "").ContinueWith(t => t.Result.Count);
                roleDtos.Add(new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty,
                    UserCount = userCount
                });
            }

            return roleDtos;
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
            if (role == null) return false;

            // Don't allow deleting default roles
            if (RoleNames.AllRoles.Contains(role.Name)) return false;

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded;
        }
    }
}