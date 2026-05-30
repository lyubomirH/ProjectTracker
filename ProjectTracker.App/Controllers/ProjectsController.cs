using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;
using ProjectTracker.Web.ViewModels.Projects;
using ProjectTracker.Web.ViewModels.Shared;
using ProjectTracker.Web.ViewModels.WorkItems;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IWorkItemService _workItemService;
        private readonly ITeamService _teamService;

        public ProjectsController(
            IProjectService projectService,
            IWorkItemService workItemService,
            ITeamService teamService)
        {
            _projectService = projectService;
            _workItemService = workItemService;
            _teamService = teamService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(ProjectFilterViewModel filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            // Set default page size
            if (filter.PageSize <= 0) filter.PageSize = 6;
            if (filter.Page < 1) filter.Page = 1;

            var filterDto = new ProjectFilterDto
            {
                SearchTerm = filter.SearchTerm,
                Status = filter.Status,
                SortBy = filter.SortBy,
                SortDescending = filter.SortDescending,
                Page = filter.Page,
                PageSize = filter.PageSize,
                UserId = userId ?? string.Empty,
                IsAdmin = isAdmin
            };

            var result = await _projectService.GetFilteredProjectsAsync(filterDto);

            var viewModel = new ProjectIndexViewModel
            {
                Projects = result.Items.Select(p => new ProjectListViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    OwnerName = p.OwnerName,
                    TeamMembersCount = p.TeamMembersCount,
                    WorkItemsCount = p.WorkItemsCount,
                    CompletedWorkItemsCount = p.CompletedWorkItemsCount,
                    CreatedAt = p.CreatedAt
                }).ToList(),
                Filter = new ViewModels.Projects.ProjectFilterViewModel
                {
                    SearchTerm = filter.SearchTerm,
                    Status = filter.Status,
                    SortBy = filter.SortBy,
                    SortDescending = filter.SortDescending,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                },
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
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Create()
        {
            var model = new ProjectFormViewModel
            {
                StartDate = DateTime.Today,
                Status = "Active"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create(ProjectFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var createDto = new CreateProjectDto
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = model.Status
            };

            var project = await _projectService.CreateProjectAsync(createDto, userId);

            TempData["SuccessMessage"] = $"Project \"{project.Name}\" created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId) && !isAdmin)
            {
                return RedirectToAction("Login", "Auth");
            }

            var project = await _projectService.GetProjectByIdAsync(id, userId ?? string.Empty, isAdmin);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Get team members
            var teamMembers = await _teamService.GetTeamMembersAsync(id);

            // Get work items for this project
            var workItems = await _workItemService.GetWorkItemsAsync(id, userId ?? string.Empty, isAdmin);

            // Determine current user's role in the project
            var currentUserRole = "Viewer";
            if (isAdmin)
            {
                currentUserRole = "Admin";
            }
            else if (project.OwnerId == userId)
            {
                currentUserRole = "Owner";
            }
            else
            {
                var userTeamMember = teamMembers.FirstOrDefault(tm => tm.UserId == userId);
                if (userTeamMember != null)
                {
                    currentUserRole = userTeamMember.Role;
                }
            }

            var viewModel = new ProjectDetailsViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                OwnerName = project.OwnerName,
                CreatedAt = project.CreatedAt,
                CurrentUserRole = currentUserRole,
                TeamMembers = teamMembers.Select(tm => new TeamMemberViewModel
                {
                    UserId = tm.UserId,
                    UserName = tm.UserName,
                    Role = tm.Role,
                    JoinedAt = tm.JoinedAt
                }).ToList(),
                WorkItems = workItems.Select(w => new WorkItemSummaryViewModel
                {
                    Id = w.Id,
                    Title = w.Title,
                    Status = w.Status,
                    Priority = w.Priority,
                    AssigneeName = w.AssigneeName,
                    DueDate = w.DueDate
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var project = await _projectService.GetProjectByIdAsync(id, userId, isAdmin);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var model = new ProjectFormViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(ProjectFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var updateDto = new UpdateProjectDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = model.Status
            };

            var result = await _projectService.UpdateProjectAsync(updateDto, userId, isAdmin);

            if (result == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            TempData["SuccessMessage"] = $"Project \"{result.Name}\" updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var result = await _projectService.DeleteProjectAsync(id, userId, isAdmin);

            if (!result)
            {
                return RedirectToAction("Error404", "Home");
            }

            TempData["SuccessMessage"] = "Project deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> AddTeamMember(int projectId, string userId, string role)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var canManage = await _teamService.CanUserManageTeamAsync(projectId, currentUserId ?? string.Empty);
            if (!canManage && !isAdmin)
            {
                return Json(new { success = false, message = "You don't have permission to add team members." });
            }

            try
            {
                var teamMember = await _teamService.AddTeamMemberAsync(projectId, userId, role, currentUserId ?? string.Empty);
                return Json(new { success = true, message = "Team member added successfully!", teamMember });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> RemoveTeamMember(int projectId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var canManage = await _teamService.CanUserManageTeamAsync(projectId, currentUserId ?? string.Empty);
            if (!canManage && !isAdmin)
            {
                return Json(new { success = false, message = "You don't have permission to remove team members." });
            }

            var project = await _projectService.GetProjectByIdAsync(projectId, currentUserId ?? string.Empty, isAdmin);
            if (project != null && project.OwnerId == userId)
            {
                return Json(new { success = false, message = "Cannot remove the project owner." });
            }

            var result = await _teamService.RemoveTeamMemberAsync(projectId, userId, currentUserId ?? string.Empty);

            if (result)
            {
                return Json(new { success = true, message = "Team member removed successfully!" });
            }

            return Json(new { success = false, message = "Failed to remove team member." });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ManageTeam(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _projectService.GetProjectByIdAsync(id, userId ?? string.Empty, isAdmin);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Провери дали потребителят може да управлява екипа
            var canManage = await _teamService.CanUserManageTeamAsync(id, userId ?? string.Empty);
            if (!canManage && !isAdmin)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var teamMembers = await _teamService.GetTeamMembersAsync(id);
            var availableUsers = await _teamService.GetAvailableUsersForProjectAsync(id);
            var projectManagers = await _teamService.GetProjectManagersAsync();

            var viewModel = new ManageTeamViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                TeamMembers = teamMembers.ToList(),
                AvailableUsers = availableUsers.ToList(),
                ProjectManagers = projectManagers.ToList(),
                CurrentUserRole = await GetUserRoleInProject(id, userId ?? string.Empty)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateTeamMemberRole(int projectId, string userId, string role)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var canManage = await _teamService.CanUserManageTeamAsync(projectId, currentUserId ?? string.Empty);
            if (!canManage && !isAdmin)
            {
                return Json(new { success = false, message = "You don't have permission to update team member roles." });
            }

            var project = await _projectService.GetProjectByIdAsync(projectId, currentUserId ?? string.Empty, isAdmin);
            if (project != null && project.OwnerId == userId)
            {
                return Json(new { success = false, message = "Cannot change the project owner's role." });
            }

            var result = await _teamService.UpdateTeamMemberRoleAsync(projectId, userId, role, currentUserId ?? string.Empty);

            if (result)
            {
                return Json(new { success = true, message = "Team member role updated successfully!" });
            }

            return Json(new { success = false, message = "Failed to update team member role." });
        }

        private async Task<string> GetUserRoleInProject(int projectId, string userId)
        {
            var teamMember = await _teamService.GetTeamMemberAsync(projectId, userId);
            if (teamMember != null)
            {
                return teamMember.Role;
            }

            var project = await _projectService.GetProjectByIdAsync(projectId, userId, false);
            if (project != null && project.OwnerId == userId)
            {
                return "Owner";
            }

            return "None";
        }
    }
}