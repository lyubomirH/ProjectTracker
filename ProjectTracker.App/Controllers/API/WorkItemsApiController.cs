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
    public class WorkItemsApiController : ControllerBase
    {
        private readonly IWorkItemService _workItemService;

        public WorkItemsApiController(IWorkItemService workItemService)
        {
            _workItemService = workItemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkItems([FromQuery] WorkItemFilterDto filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            filter.UserId = userId ?? string.Empty;
            filter.IsAdmin = isAdmin;

            var result = await _workItemService.GetFilteredWorkItemsAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWorkItem(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var workItem = await _workItemService.GetWorkItemByIdAsync(id, userId ?? string.Empty, isAdmin);

            if (workItem == null)
            {
                return NotFound();
            }

            return Ok(workItem);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateWorkItemStatusDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var result = await _workItemService.UpdateWorkItemStatusAsync(id, model.Status, userId ?? string.Empty, isAdmin);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new { success = true, message = "Status updated successfully" });
        }

        [HttpPost("{id}/assign")]
        public async Task<IActionResult> AssignWorkItem(int id, [FromBody] AssignWorkItemDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var result = await _workItemService.AssignWorkItemAsync(id, model.AssigneeId, userId ?? string.Empty, isAdmin);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new { success = true, message = "Work item assigned successfully" });
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var comment = await _workItemService.AddCommentAsync(id, model.Content, userId);

            return Ok(new { success = true, comment });
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            var comments = await _workItemService.GetCommentsAsync(id);
            return Ok(comments);
        }
    }

    public class AssignWorkItemDto
    {
        public string? AssigneeId { get; set; }
    }
}