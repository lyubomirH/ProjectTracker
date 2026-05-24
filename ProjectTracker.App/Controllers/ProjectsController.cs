using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;
using ProjectTracker.Web.ViewModels.Projects;
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

            if (string.IsNullOrEmpty(userId) && !isAdmin)
            {
                return View(new ProjectIndexViewModel());
            }

            // Фиксирай PageSize на 6 за Projects
            filter.PageSize = 6;

            var filterDto = new ProjectFilterDto
            {
                SearchTerm = filter.SearchTerm,
                Status = filter.Status,
                SortBy = filter.SortBy,
                SortDescending = filter.SortDescending,
                Page = filter.Page,
                PageSize = filter.PageSize,  // Сега е 6
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
                Filter = filter,
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

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Check if current user has permission
            var project = await _projectService.GetProjectByIdAsync(projectId, currentUserId, isAdmin);
            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            await _teamService.AddTeamMemberAsync(projectId, userId, role, currentUserId);

            TempData["SuccessMessage"] = "Team member added successfully!";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> RemoveTeamMember(int projectId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            await _teamService.RemoveTeamMemberAsync(projectId, userId, currentUserId);

            TempData["SuccessMessage"] = "Team member removed successfully!";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }
    }
}