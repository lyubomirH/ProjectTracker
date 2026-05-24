using ProjectTracker.Services.DTOs;

namespace ProjectTracker.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardDataAsync(string userId, bool isAdmin);
        Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(string userId, bool isAdmin, int count = 10);
        Task<IEnumerable<ProjectProgressDto>> GetProjectProgressAsync(string userId, bool isAdmin);
    }
}