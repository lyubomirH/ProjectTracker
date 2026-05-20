using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Web.ViewModels.Projects;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var projectsQuery = _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.TeamMembers)
                .Where(p => !p.IsDeleted);

            // Non-admin users see only projects they own or are team members of
            if (!isAdmin)
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.OwnerId == userId ||
                    p.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var projects = await projectsQuery
                .Select(p => new ProjectListViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status.ToString(),
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    OwnerName = p.Owner.FullName,
                    TeamMembersCount = p.TeamMembers.Count,
                    WorkItemsCount = p.WorkItems.Count,
                    CompletedWorkItemsCount = p.WorkItems.Count(w => w.Status == WorkItemStatus.Done)
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Create()
        {
            var model = new ProjectFormViewModel
            {
                StartDate = DateTime.Today,
                Status = ProjectStatus.Active.ToString()
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

            var project = new Project
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = Enum.Parse<ProjectStatus>(model.Status),
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Add owner as team member with ProjectManager role
            var teamMember = new TeamMember
            {
                ProjectId = project.Id,
                UserId = userId,
                Role = TeamRole.ProjectManager,
                JoinedAt = DateTime.UtcNow
            };
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Project created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.TeamMembers)
                    .ThenInclude(tm => tm.User)
                .Include(p => p.WorkItems)
                    .ThenInclude(w => w.Assignee)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Check access
            var isOwner = project.OwnerId == userId;
            var isTeamMember = project.TeamMembers.Any(tm => tm.UserId == userId);

            if (!isAdmin && !isOwner && !isTeamMember)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var model = new ProjectDetailsViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status.ToString(),
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                OwnerName = project.Owner.FullName,
                CreatedAt = project.CreatedAt,
                TeamMembers = project.TeamMembers.Select(tm => new TeamMemberViewModel
                {
                    UserId = tm.UserId,
                    UserName = tm.User.FullName,
                    Role = tm.Role.ToString(),
                    JoinedAt = tm.JoinedAt
                }).ToList(),
                WorkItems = project.WorkItems.Select(w => new WorkItemSummaryViewModel
                {
                    Id = w.Id,
                    Title = w.Title,
                    Status = w.Status.ToString(),
                    Priority = w.Priority.ToString(),
                    AssigneeName = w.Assignee != null ? w.Assignee.FullName : "Unassigned",
                    DueDate = w.DueDate
                }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Check permission
            if (!isAdmin && project.OwnerId != userId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var model = new ProjectFormViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status.ToString()
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

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == model.Id && !p.IsDeleted);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Check permission
            if (!isAdmin && project.OwnerId != userId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            project.Name = model.Name;
            project.Description = model.Description;
            project.StartDate = model.StartDate;
            project.EndDate = model.EndDate;
            project.Status = Enum.Parse<ProjectStatus>(model.Status);
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Project updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (project == null)
            {
                return RedirectToAction("Error404", "Home");
            }

            // Check permission
            if (!isAdmin && project.OwnerId != userId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Project deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}