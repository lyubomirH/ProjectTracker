using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;
using ProjectTracker.Web.ViewModels.WorkItems;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers
{
    [Authorize]
    public class WorkItemsController : Controller
    {
        private readonly IWorkItemService _workItemService;
        private readonly IProjectService _projectService;
        private readonly ApplicationDbContext _context;

        public WorkItemsController(
            IWorkItemService workItemService,
            IProjectService projectService,
            ApplicationDbContext context)
        {
            _workItemService = workItemService;
            _projectService = projectService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? projectId, string? searchTerm, string? status,
    string? priority, string? assigneeId, string? sortBy, bool sortDescending = true,
    int page = 1, int pageSize = 10)  // pageSize default = 10
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId) && !isAdmin)
            {
                return View(new WorkItemIndexViewModel());
            }

            // Фиксирай PageSize на 10 за WorkItems (презаписва каквото е подадено)
            pageSize = 10;

            var filterDto = new WorkItemFilterDto
            {
                SearchTerm = searchTerm,
                Status = status,
                Priority = priority,
                ProjectId = projectId,
                AssigneeId = assigneeId,
                SortBy = sortBy ?? "CreatedAt",
                SortDescending = sortDescending,
                Page = page,
                PageSize = pageSize,  // Сега е 10
                UserId = userId ?? string.Empty,
                IsAdmin = isAdmin
            };

            var result = await _projectService.GetFilteredWorkItemsAsync(filterDto);

            // Get projects for filter dropdown
            var projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Select(p => new ProjectDropdownViewModel { Id = p.Id, Name = p.Name })
                .ToListAsync();

            var viewModel = new WorkItemIndexViewModel
            {
                WorkItems = result.Items.Select(w => new WorkItemListViewModel
                {
                    Id = w.Id,
                    Title = w.Title,
                    ProjectName = w.ProjectName,
                    ProjectId = w.ProjectId,
                    Status = w.Status,
                    Priority = w.Priority,
                    AssigneeName = w.AssigneeName,
                    CreatedAt = w.CreatedAt,
                    DueDate = w.DueDate
                }).ToList(),
                Filter = new WorkItemFilterViewModel
                {
                    SearchTerm = searchTerm,
                    Status = status,
                    Priority = priority,
                    ProjectId = projectId,
                    AssigneeId = assigneeId,
                    SortBy = sortBy ?? "CreatedAt",
                    SortDescending = sortDescending,
                    Page = page,
                    PageSize = pageSize
                },
                Pagination = new PaginationViewModel
                {
                    CurrentPage = result.Page,
                    TotalPages = result.TotalPages,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount
                },
                Projects = projects
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _projectService.GetProjectByIdAsync(projectId, userId ?? string.Empty, isAdmin);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Get team members for assignee dropdown
            var teamMembers = await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.ProjectId == projectId && tm.IsActive)
                .Select(tm => new { tm.UserId, tm.User.FullName })
                .ToListAsync();

            ViewBag.TeamMembers = teamMembers;

            var model = new WorkItemFormViewModel
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                Priority = "Medium",
                Status = "ToDo",
                DueDate = DateTime.Today.AddDays(7)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkItemFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var project = await _context.Projects.FindAsync(model.ProjectId);
                model.ProjectName = project?.Name ?? "";

                var teamMembers = await _context.TeamMembers
                    .Include(tm => tm.User)
                    .Where(tm => tm.ProjectId == model.ProjectId && tm.IsActive)
                    .Select(tm => new { tm.UserId, tm.User.FullName })
                    .ToListAsync();

                ViewBag.TeamMembers = teamMembers;
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var createDto = new CreateWorkItemDto
            {
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority,
                Status = model.Status,
                ProjectId = model.ProjectId,
                AssigneeId = model.AssigneeId,
                DueDate = model.DueDate,
                EstimatedHours = model.EstimatedHours
            };

            var workItem = await _workItemService.CreateWorkItemAsync(createDto, userId ?? string.Empty);

            TempData["SuccessMessage"] = $"Work item \"{workItem.Title}\" created successfully!";
            return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItem = await _workItemService.GetWorkItemByIdAsync(id, userId ?? string.Empty, isAdmin);

            if (workItem == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var comments = await _workItemService.GetCommentsAsync(id);

            var viewModel = new WorkItemDetailsViewModel
            {
                Id = workItem.Id,
                Title = workItem.Title,
                Description = workItem.Description,
                Priority = workItem.Priority,
                Status = workItem.Status,
                ProjectId = workItem.ProjectId,
                ProjectName = workItem.ProjectName,
                AssigneeName = workItem.AssigneeName,
                CreatedByName = workItem.CreatedByName,
                CreatedAt = workItem.CreatedAt,
                DueDate = workItem.DueDate,
                EstimatedHours = workItem.EstimatedHours,
                ActualHours = workItem.ActualHours,
                CompletedAt = workItem.CompletedAt,
                Comments = comments.Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorName = c.AuthorName,
                    CreatedAt = c.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItem = await _workItemService.GetWorkItemByIdAsync(id, userId ?? string.Empty, isAdmin);

            if (workItem == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Get team members for assignee dropdown
            var teamMembers = await _context.TeamMembers
                .Include(tm => tm.User)
                .Where(tm => tm.ProjectId == workItem.ProjectId && tm.IsActive)
                .Select(tm => new { tm.UserId, tm.User.FullName })
                .ToListAsync();

            ViewBag.TeamMembers = teamMembers;

            var model = new WorkItemFormViewModel
            {
                Id = workItem.Id,
                Title = workItem.Title,
                Description = workItem.Description,
                Priority = workItem.Priority,
                Status = workItem.Status,
                ProjectId = workItem.ProjectId,
                ProjectName = workItem.ProjectName,
                AssigneeId = workItem.AssigneeId,
                DueDate = workItem.DueDate,
                EstimatedHours = workItem.EstimatedHours,
                ActualHours = workItem.ActualHours
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WorkItemFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var project = await _context.Projects.FindAsync(model.ProjectId);
                model.ProjectName = project?.Name ?? "";

                var teamMembers = await _context.TeamMembers
                    .Include(tm => tm.User)
                    .Where(tm => tm.ProjectId == model.ProjectId && tm.IsActive)
                    .Select(tm => new { tm.UserId, tm.User.FullName })
                    .ToListAsync();

                ViewBag.TeamMembers = teamMembers;
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var updateDto = new UpdateWorkItemDto
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority,
                Status = model.Status,
                AssigneeId = model.AssigneeId,
                DueDate = model.DueDate,
                EstimatedHours = model.EstimatedHours,
                ActualHours = model.ActualHours
            };

            var result = await _workItemService.UpdateWorkItemAsync(updateDto, userId ?? string.Empty, isAdmin);

            if (result == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            TempData["SuccessMessage"] = $"Work item \"{result.Title}\" updated successfully!";
            return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var result = await _workItemService.DeleteWorkItemAsync(id, userId ?? string.Empty, isAdmin);

            if (!result)
            {
                return RedirectToAction("Error404", "Home");
            }

            TempData["SuccessMessage"] = "Work item deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int workItemId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { success = false, message = "Comment content cannot be empty." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            try
            {
                var comment = await _workItemService.AddCommentAsync(workItemId, content, userId);
                return Ok(new { success = true, message = "Comment added successfully", comment });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            var result = await _workItemService.UpdateWorkItemStatusAsync(id, status, userId, isAdmin);

            if (!result)
            {
                return NotFound(new { success = false, message = "Work item not found or access denied." });
            }

            return Ok(new { success = true, message = "Status updated successfully" });
        }
    }
}