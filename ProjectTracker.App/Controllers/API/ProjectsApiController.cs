using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsApiController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IWorkItemService _workItemService;

        public ProjectsApiController(IProjectService projectService, IWorkItemService workItemService)
        {
            _projectService = projectService;
            _workItemService = workItemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] ProjectFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            filter.UserId = userId ?? string.Empty;
            filter.IsAdmin = isAdmin;

            var result = await _projectService.GetFilteredProjectsAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _projectService.GetProjectByIdAsync(id, userId ?? string.Empty, isAdmin);

            if (project == null)
            {
                return NotFound();
            }

            return Ok(project);
        }

        [HttpGet("{id}/statistics")]
        public async Task<IActionResult> GetProjectStatistics(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var project = await _projectService.GetProjectByIdAsync(id, userId ?? string.Empty, isAdmin);

            if (project == null)
            {
                return NotFound();
            }

            var statistics = new
            {
                project.Id,
                project.Name,
                project.CompletionPercentage,
                project.WorkItemsCount,
                project.CompletedWorkItemsCount,
                project.TeamMembersCount,
                ToDoCount = await _workItemService.GetWorkItemsCountByStatusAsync(id, "ToDo"),
                InProgressCount = await _workItemService.GetWorkItemsCountByStatusAsync(id, "InProgress"),
                DoneCount = await _workItemService.GetWorkItemsCountByStatusAsync(id, "Done"),
                BlockedCount = await _workItemService.GetWorkItemsCountByStatusAsync(id, "Blocked")
            };

            return Ok(statistics);
        }
    }
}