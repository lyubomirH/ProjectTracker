using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;
using System.Security.Claims;

namespace ProjectTracker.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var dashboard = await _dashboardService.GetDashboardDataAsync(userId ?? string.Empty, isAdmin);

            // Трябва да върне DashboardDto, а не WorkItemIndexViewModel
            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivities(int count = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var activities = await _dashboardService.GetRecentActivitiesAsync(userId ?? string.Empty, isAdmin, count);

            return PartialView("_RecentActivities", activities);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectProgress()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var progress = await _dashboardService.GetProjectProgressAsync(userId ?? string.Empty, isAdmin);

            return PartialView("_ProjectProgress", progress);
        }
    }
}