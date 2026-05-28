using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectTracker.Data.Constants;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;
using ProjectTracker.Web.ViewModels.Admin;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var stats = await _adminService.GetStatisticsAsync();

            var viewModel = new AdminStatisticsViewModel
            {
                TotalUsers = stats.TotalUsers,
                TotalProjects = stats.TotalProjects,
                TotalWorkItems = stats.TotalWorkItems,
                TotalComments = stats.TotalComments,
                ActiveUsers = stats.ActiveUsers,
                RecentProjects = stats.RecentProjects.Select(p => new RecentProjectViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    OwnerName = p.OwnerName,
                    CreatedAt = p.CreatedAt,
                    Status = p.Status
                }).ToList(),
                RecentUsers = stats.RecentUsers.Select(u => new RecentUserViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Users(string? searchTerm, int page = 1, int pageSize = 10)
        {
            var result = await _adminService.GetUsersAsync(searchTerm, page, pageSize);

            var viewModel = new UserListViewModel
            {
                Users = result.Items.Select(u => new UserAdminViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.IsActive,
                    Roles = u.Roles,
                    Department = u.Department,
                    JobTitle = u.JobTitle
                }).ToList(),
                SearchTerm = searchTerm,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = result.Page,
                    TotalPages = result.TotalPages,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount
                }
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Error404", "Home");
            }

            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var roles = await _adminService.GetRolesAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                Department = user.Department,
                JobTitle = user.JobTitle,
                Bio = user.Bio,
                CurrentRoles = user.Roles,
                AvailableRoles = roles.Select(r => r.Name).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var roles = await _adminService.GetRolesAsync();
                model.AvailableRoles = roles.Select(r => r.Name).ToList();
                return View(model);
            }

            var userDto = new EditUserDto
            {
                Id = model.Id,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                IsActive = model.IsActive,
                Department = model.Department,
                JobTitle = model.JobTitle,
                Bio = model.Bio,
                SelectedRoles = model.SelectedRoles
            };

            var result = await _adminService.UpdateUserAsync(userDto);

            if (!result)
            {
                TempData["ErrorMessage"] = "Error updating user.";
                return RedirectToAction(nameof(Users));
            }

            TempData["SuccessMessage"] = $"User {model.FullName} updated successfully!";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var result = await _adminService.DeleteUserAsync(id, currentUserId);

            if (!result)
            {
                TempData["ErrorMessage"] = "Cannot delete this user.";
                return RedirectToAction(nameof(Users));
            }

            TempData["SuccessMessage"] = "User deleted successfully!";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Projects(string? searchTerm, string? status, int page = 1, int pageSize = 10)
        {
            var result = await _adminService.GetProjectsAsync(searchTerm, status, page, pageSize);

            var viewModel = new AdminProjectListViewModel
            {
                Projects = result.Items.Select(p => new AdminProjectViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    OwnerName = p.OwnerName,
                    WorkItemsCount = p.WorkItemsCount,
                    CreatedAt = p.CreatedAt,
                    IsDeleted = p.IsDeleted
                }).ToList(),
                SearchTerm = searchTerm,
                Status = status,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = result.Page,
                    TotalPages = result.TotalPages,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount
                },
                Statuses = new List<string> { "All", "Active", "OnHold", "Completed", "Archived", "Cancelled" }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProjectStatus(int id, string status)
        {
            var result = await _adminService.UpdateProjectStatusAsync(id, status);

            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"Project status updated to {status}!";
            return RedirectToAction(nameof(Projects));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDeleteProject(int id)
        {
            var result = await _adminService.HardDeleteProjectAsync(id);

            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "Project permanently deleted!";
            return RedirectToAction(nameof(Projects));
        }

        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _adminService.GetRolesAsync();

            var viewModel = new RoleListViewModel
            {
                Roles = roles.Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    UserCount = r.UserCount
                }).ToList(),
                TotalUsers = await _adminService.GetUsersAsync(null, 1, 1).ContinueWith(t => t.Result.TotalCount)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var result = await _adminService.CreateRoleAsync(roleName);

            if (!result)
            {
                TempData["ErrorMessage"] = $"Role {roleName} already exists or invalid.";
                return RedirectToAction(nameof(Roles));
            }

            TempData["SuccessMessage"] = $"Role {roleName} created successfully!";
            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var result = await _adminService.DeleteRoleAsync(id);

            if (!result)
            {
                TempData["ErrorMessage"] = "Cannot delete this role.";
                return RedirectToAction(nameof(Roles));
            }

            TempData["SuccessMessage"] = "Role deleted successfully!";
            return RedirectToAction(nameof(Roles));
        }
    }
}