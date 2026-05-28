using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Constants;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.Interfaces;
using ProjectTracker.Web.ViewModels.Admin;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IDashboardService _dashboardService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IDashboardService dashboardService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var stats = new AdminStatisticsViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalProjects = await _context.Projects.CountAsync(p => !p.IsDeleted),
                TotalWorkItems = await _context.WorkItems.CountAsync(),
                TotalComments = await _context.Comments.CountAsync(),
                ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive),
                RecentProjects = await _context.Projects
                    .Include(p => p.Owner)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .Select(p => new RecentProjectViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        OwnerName = p.Owner.FullName,
                        CreatedAt = p.CreatedAt,
                        Status = p.Status.ToString()
                    }).ToListAsync(),
                RecentUsers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new RecentUserViewModel
                    {
                        Id = u.Id,
                        Email = u.Email ?? string.Empty,
                        FullName = u.FullName,
                        CreatedAt = u.CreatedAt,
                        IsActive = u.IsActive
                    }).ToListAsync()
            };

            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> Users(string? searchTerm, int page = 1, int pageSize = 10)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Email.Contains(searchTerm) ||
                                         u.FullName.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userViewModels = new List<UserAdminViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserAdminViewModel
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
                    JobTitle = user.JobTitle
                });
            }

            var viewModel = new UserListViewModel
            {
                Users = userViewModels,
                SearchTerm = searchTerm,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    PageSize = pageSize,
                    TotalCount = totalCount
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

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                Department = user.Department,
                JobTitle = user.JobTitle,
                Bio = user.Bio,
                CurrentRoles = roles.ToList(),
                AvailableRoles = allRoles
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.IsActive = model.IsActive;
            user.Department = model.Department;
            user.JobTitle = model.JobTitle;
            user.Bio = model.Bio;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            // Update roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = model.SelectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(model.SelectedRoles).ToList();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }
            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            TempData["SuccessMessage"] = $"User {user.FullName} updated successfully!";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Don't allow deleting yourself
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User {user.FullName} deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting user.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Projects(string? searchTerm, string? status, int page = 1, int pageSize = 10)
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

            var viewModel = new AdminProjectListViewModel
            {
                Projects = projects.Select(p => new AdminProjectViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status.ToString(),
                    OwnerName = p.Owner?.FullName ?? "Unknown",
                    WorkItemsCount = p.WorkItems.Count,
                    CreatedAt = p.CreatedAt,
                    IsDeleted = p.IsDeleted
                }).ToList(),
                SearchTerm = searchTerm,
                Status = status,
                Pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                Statuses = new List<string> { "All", "Active", "OnHold", "Completed", "Archived", "Cancelled" }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProjectStatus(int id, string status)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            project.Status = Enum.Parse<ProjectStatus>(status);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Project status updated to {status}!";
            return RedirectToAction(nameof(Projects));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDeleteProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.WorkItems)
                .Include(p => p.TeamMembers)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Delete related work items first
            _context.WorkItems.RemoveRange(project.WorkItems);
            _context.TeamMembers.RemoveRange(project.TeamMembers);
            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Project {project.Name} permanently deleted!";
            return RedirectToAction(nameof(Projects));
        }

        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var usersCount = await _userManager.Users.CountAsync();

            var viewModel = new RoleListViewModel
            {
                Roles = roles.Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    UserCount = _userManager.GetUsersInRoleAsync(r.Name ?? "").Result.Count
                }).ToList(),
                TotalUsers = usersCount
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["ErrorMessage"] = "Role name cannot be empty.";
                return RedirectToAction(nameof(Roles));
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                TempData["SuccessMessage"] = $"Role {roleName} created successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Role {roleName} already exists.";
            }

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Don't allow deleting default roles
            if (RoleNames.AllRoles.Contains(role.Name))
            {
                TempData["ErrorMessage"] = $"Cannot delete default role {role.Name}.";
                return RedirectToAction(nameof(Roles));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role {role.Name} deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting role.";
            }

            return RedirectToAction(nameof(Roles));
        }
    }
}