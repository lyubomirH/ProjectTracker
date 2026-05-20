using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Web.ViewModels.WorkItems;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers
{
    [Authorize]
    public class WorkItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItemsQuery = _context.WorkItems
                .Include(w => w.Project)
                .Include(w => w.Assignee)
                .Include(w => w.CreatedBy)
                .Where(w => !w.Project.IsDeleted);

            if (projectId.HasValue)
            {
                workItemsQuery = workItemsQuery.Where(w => w.ProjectId == projectId.Value);
            }

            if (!isAdmin)
            {
                workItemsQuery = workItemsQuery.Where(w =>
                    w.AssigneeId == userId ||
                    w.CreatedById == userId ||
                    w.Project.OwnerId == userId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var workItems = await workItemsQuery
                .Select(w => new WorkItemListViewModel
                {
                    Id = w.Id,
                    Title = w.Title,
                    ProjectName = w.Project.Name,
                    ProjectId = w.ProjectId,
                    Status = w.Status.ToString(),
                    Priority = w.Priority.ToString(),
                    AssigneeName = w.Assignee != null ? w.Assignee.FullName : "Unassigned",
                    CreatedAt = w.CreatedAt,
                    DueDate = w.DueDate
                })
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            ViewBag.Projects = projects;
            ViewBag.SelectedProjectId = projectId;

            return View(workItems);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _context.Projects
                .Include(p => p.TeamMembers)
                .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var isOwner = project.OwnerId == userId;
            var isTeamMember = project.TeamMembers.Any(tm => tm.UserId == userId);

            if (!isAdmin && !isOwner && !isTeamMember)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

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
                Priority = WorkItemPriority.Medium.ToString(),
                Status = WorkItemStatus.ToDo.ToString(),
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

            var workItem = new WorkItem
            {
                Title = model.Title,
                Description = model.Description,
                Priority = Enum.Parse<WorkItemPriority>(model.Priority),
                Status = Enum.Parse<WorkItemStatus>(model.Status),
                ProjectId = model.ProjectId,
                AssigneeId = model.AssigneeId,
                CreatedById = userId,
                DueDate = model.DueDate,
                EstimatedHours = model.EstimatedHours,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkItems.Add(workItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Work item created successfully!";
            return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .Include(w => w.Assignee)
                .Include(w => w.CreatedBy)
                .Include(w => w.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId && workItem.CreatedById != userId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var model = new WorkItemDetailsViewModel
            {
                Id = workItem.Id,
                Title = workItem.Title,
                Description = workItem.Description,
                Priority = workItem.Priority.ToString(),
                Status = workItem.Status.ToString(),
                ProjectId = workItem.ProjectId,
                ProjectName = workItem.Project.Name,
                AssigneeName = workItem.Assignee?.FullName ?? "Unassigned",
                CreatedByName = workItem.CreatedBy?.FullName ?? "Unknown",
                CreatedAt = workItem.CreatedAt,
                DueDate = workItem.DueDate,
                EstimatedHours = workItem.EstimatedHours,
                ActualHours = workItem.ActualHours,
                CompletedAt = workItem.CompletedAt,
                Comments = workItem.Comments.Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorName = c.Author.FullName,
                    CreatedAt = c.CreatedAt
                }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

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
                Priority = workItem.Priority.ToString(),
                Status = workItem.Status.ToString(),
                ProjectId = workItem.ProjectId,
                ProjectName = workItem.Project.Name,
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

            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == model.Id && !w.Project.IsDeleted);

            if (workItem == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var oldStatus = workItem.Status;
            var newStatus = Enum.Parse<WorkItemStatus>(model.Status);

            workItem.Title = model.Title;
            workItem.Description = model.Description;
            workItem.Priority = Enum.Parse<WorkItemPriority>(model.Priority);
            workItem.Status = newStatus;
            workItem.AssigneeId = model.AssigneeId;
            workItem.DueDate = model.DueDate;
            workItem.EstimatedHours = model.EstimatedHours;
            workItem.ActualHours = model.ActualHours;

            if (oldStatus != WorkItemStatus.Done && newStatus == WorkItemStatus.Done)
            {
                workItem.CompletedAt = DateTime.UtcNow;
            }
            else if (newStatus != WorkItemStatus.Done)
            {
                workItem.CompletedAt = null;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Work item updated successfully!";
            return RedirectToAction(nameof(Index), new { projectId = workItem.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            var isOwner = workItem.Project.OwnerId == userId;

            if (!isAdmin && !isOwner)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            _context.WorkItems.Remove(workItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Work item deleted successfully!";
            return RedirectToAction(nameof(Index), new { projectId = workItem.ProjectId });
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
            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == workItemId);

            if (workItem == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Admin");
            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId && workItem.CreatedById != userId)
            {
                return Unauthorized();
            }

            var comment = new Comment
            {
                Content = content,
                WorkItemId = workItemId,
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { success = true, message = "Comment added successfully" });
            }

            return RedirectToAction(nameof(Details), new { id = workItemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItem = await _context.WorkItems
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id && !w.Project.IsDeleted);

            if (workItem == null)
            {
                return NotFound();
            }

            var isOwner = workItem.Project.OwnerId == userId;
            var isTeamMember = await _context.TeamMembers
                .AnyAsync(tm => tm.ProjectId == workItem.ProjectId && tm.UserId == userId);

            if (!isAdmin && !isOwner && !isTeamMember && workItem.AssigneeId != userId)
            {
                return Unauthorized();
            }

            var oldStatus = workItem.Status;
            var newStatus = Enum.Parse<WorkItemStatus>(status);

            workItem.Status = newStatus;

            if (oldStatus != WorkItemStatus.Done && newStatus == WorkItemStatus.Done)
            {
                workItem.CompletedAt = DateTime.UtcNow;
            }
            else if (newStatus != WorkItemStatus.Done)
            {
                workItem.CompletedAt = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, status = status });
        }
    }
}