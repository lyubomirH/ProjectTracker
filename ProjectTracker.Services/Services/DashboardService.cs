using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;

namespace ProjectTracker.Services.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDto> GetDashboardDataAsync(string userId, bool isAdmin)
        {
            var projectsQuery = _context.Projects
                .Where(p => !p.IsDeleted);

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.OwnerId == userId ||
                    p.TeamMembers.Any(tm => tm.UserId == userId));
            }

            // Optimized - Get project counts with aggregation
            var projectData = await projectsQuery
                .GroupBy(p => 1)
                .Select(g => new
                {
                    TotalProjects = g.Count(),
                    ActiveProjects = g.Count(p => p.Status == ProjectStatus.Active),
                    CompletedProjects = g.Count(p => p.Status == ProjectStatus.Completed),
                    OnHoldProjects = g.Count(p => p.Status == ProjectStatus.OnHold),
                    ProjectsByStatus = g.GroupBy(p => p.Status)
                        .Select(sg => new ProjectByStatusDto
                        {
                            Status = sg.Key.ToString(),
                            Count = sg.Count(),
                            Color = GetProjectStatusColor(sg.Key)
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            var workItemsQuery = _context.WorkItems
                .Where(w => !w.Project.IsDeleted);

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                workItemsQuery = workItemsQuery.Where(w =>
                    w.AssigneeId == userId ||
                    w.CreatedById == userId ||
                    w.Project.OwnerId == userId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == userId));
            }

            // Optimized - Get work item counts with aggregation
            var workItemData = await workItemsQuery
                .GroupBy(w => 1)
                .Select(g => new
                {
                    TotalWorkItems = g.Count(),
                    CompletedWorkItems = g.Count(w => w.Status == WorkItemStatus.Done),
                    InProgressWorkItems = g.Count(w => w.Status == WorkItemStatus.InProgress),
                    ToDoWorkItems = g.Count(w => w.Status == WorkItemStatus.ToDo),
                    BlockedWorkItems = g.Count(w => w.Status == WorkItemStatus.Blocked),
                    WorkItemsByStatus = g.GroupBy(w => w.Status)
                        .Select(sg => new WorkItemByStatusDto
                        {
                            Status = sg.Key.ToString(),
                            Count = sg.Count(),
                            Color = GetWorkItemStatusColor(sg.Key)
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            var teamMembersCount = await _context.TeamMembers
                .Where(tm => tm.IsActive)
                .Select(tm => tm.UserId)
                .Distinct()
                .CountAsync();

            var dashboard = new DashboardDto
            {
                TotalProjects = projectData?.TotalProjects ?? 0,
                ActiveProjects = projectData?.ActiveProjects ?? 0,
                CompletedProjects = projectData?.CompletedProjects ?? 0,
                OnHoldProjects = projectData?.OnHoldProjects ?? 0,

                TotalWorkItems = workItemData?.TotalWorkItems ?? 0,
                CompletedWorkItems = workItemData?.CompletedWorkItems ?? 0,
                InProgressWorkItems = workItemData?.InProgressWorkItems ?? 0,
                ToDoWorkItems = workItemData?.ToDoWorkItems ?? 0,
                BlockedWorkItems = workItemData?.BlockedWorkItems ?? 0,

                TotalTeamMembers = teamMembersCount,

                WorkItemsByStatus = workItemData?.WorkItemsByStatus ?? new List<WorkItemByStatusDto>(),
                ProjectsByStatus = projectData?.ProjectsByStatus ?? new List<ProjectByStatusDto>()
            };

            dashboard.RecentActivities = await GetRecentActivitiesAsync(userId ?? string.Empty, isAdmin, 10);
            dashboard.ProjectProgress = await GetProjectProgressAsync(userId ?? string.Empty, isAdmin);

            return dashboard;
        }

        public async Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(string userId, bool isAdmin, int count = 10)
        {
            var activities = new List<RecentActivityDto>();

            // Optimized project activities with Select
            var projectsQuery = _context.Projects
                .Where(p => !p.IsDeleted);

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.OwnerId == userId ||
                    p.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var recentProjects = await projectsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new RecentActivityDto
                {
                    Id = p.Id,
                    Title = p.Name,
                    Type = "Project",
                    Action = "Created",
                    UserName = p.Owner != null ? p.Owner.FullName : "Unknown",
                    CreatedAt = p.CreatedAt,
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    Icon = "fa-project-diagram",
                    Color = "success"
                })
                .ToListAsync();

            activities.AddRange(recentProjects);

            // Optimized work item activities with Select
            var workItemsQuery = _context.WorkItems
                .Where(w => !w.Project.IsDeleted);

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                workItemsQuery = workItemsQuery.Where(w =>
                    w.AssigneeId == userId ||
                    w.CreatedById == userId ||
                    w.Project.OwnerId == userId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var recentWorkItems = await workItemsQuery
                .OrderByDescending(w => w.CreatedAt)
                .Take(count)
                .Select(w => new RecentActivityDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Type = "WorkItem",
                    Action = "Created",
                    UserName = w.CreatedBy != null ? w.CreatedBy.FullName : "Unknown",
                    CreatedAt = w.CreatedAt,
                    ProjectId = w.ProjectId,
                    ProjectName = w.Project.Name,
                    Icon = "fa-tasks",
                    Color = "primary"
                })
                .ToListAsync();

            activities.AddRange(recentWorkItems);

            // Optimized completed work items with Select
            var completedWorkItems = await workItemsQuery
                .Where(w => w.Status == WorkItemStatus.Done && w.CompletedAt.HasValue)
                .OrderByDescending(w => w.CompletedAt)
                .Take(count)
                .Select(w => new RecentActivityDto
                {
                    Id = w.Id,
                    Title = w.Title,
                    Type = "WorkItem",
                    Action = "Completed",
                    UserName = w.Assignee != null ? w.Assignee.FullName : "Unknown",
                    CreatedAt = w.CompletedAt ?? w.CreatedAt,
                    ProjectId = w.ProjectId,
                    ProjectName = w.Project.Name,
                    Icon = "fa-check-circle",
                    Color = "success"
                })
                .ToListAsync();

            activities.AddRange(completedWorkItems);

            return activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToList();
        }

        public async Task<IEnumerable<ProjectProgressDto>> GetProjectProgressAsync(string userId, bool isAdmin)
        {
            var projectsQuery = _context.Projects
                .Where(p => !p.IsDeleted && p.Status == ProjectStatus.Active);

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.OwnerId == userId ||
                    p.TeamMembers.Any(tm => tm.UserId == userId));
            }

            // Optimized with Select - calculate completion directly in query
            var projects = await projectsQuery
                .Select(p => new ProjectProgressDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    TotalTasks = p.WorkItems.Count,
                    CompletedTasks = p.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                    CompletionPercentage = p.WorkItems.Count > 0
                        ? (double)p.WorkItems.Count(w => w.Status == WorkItemStatus.Done) / p.WorkItems.Count * 100
                        : 0,
                    Status = p.Status.ToString(),
                    EndDate = p.EndDate
                })
                .OrderByDescending(p => p.CompletionPercentage)
                .Take(5)
                .ToListAsync();

            return projects;
        }

        private string GetProjectStatusColor(ProjectStatus status)
        {
            return status switch
            {
                ProjectStatus.Active => "success",
                ProjectStatus.OnHold => "warning",
                ProjectStatus.Completed => "info",
                ProjectStatus.Archived => "secondary",
                ProjectStatus.Cancelled => "danger",
                _ => "secondary"
            };
        }

        private string GetWorkItemStatusColor(WorkItemStatus status)
        {
            return status switch
            {
                WorkItemStatus.ToDo => "secondary",
                WorkItemStatus.InProgress => "primary",
                WorkItemStatus.CodeReview => "info",
                WorkItemStatus.Testing => "warning",
                WorkItemStatus.Done => "success",
                WorkItemStatus.Blocked => "danger",
                _ => "secondary"
            };
        }
    }
}