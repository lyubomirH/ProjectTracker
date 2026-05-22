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
        public async Task<IActionResult> Index(int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItems = await _workItemService.GetWorkItemsAsync(projectId, userId, isAdmin);

            var viewModel = workItems.Select(w => new WorkItemListViewModel
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
            }).ToList();

            // Get projects for filter dropdown
            var projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            ViewBag.Projects = projects;
            ViewBag.SelectedProjectId = projectId;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _projectService.GetProjectByIdAsync(projectId, userId, isAdmin);

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

            var workItem = await _workItemService.CreateWorkItemAsync(createDto, userId);

            TempData["SuccessMessage"] = $"Work item \"{workItem.Title}\" created successfully!";
            return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItem = await _workItemService.GetWorkItemByIdAsync(id, userId, isAdmin);

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

            var workItem = await _workItemService.GetWorkItemByIdAsync(id, userId, isAdmin);

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

            var result = await _workItemService.UpdateWorkItemAsync(updateDto, userId, isAdmin);

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

            var result = await _workItemService.DeleteWorkItemAsync(id, userId, isAdmin);

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
                return BadRequest("Comment content cannot be empty.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var comment = await _workItemService.AddCommentAsync(workItemId, content, userId);
                return Ok(new { success = true, message = "Comment added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}