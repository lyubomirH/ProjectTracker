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
            // Projects query
            var projectsQuery = _context.Projects
                .Include(p => p.WorkItems)
                .Where(p => !p.IsDeleted);

            if (!isAdmin)
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.OwnerId == userId ||
                    p.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var projects = await projectsQuery.ToListAsync();

            // WorkItems query
            var workItemsQuery = _context.WorkItems
                .Include(w => w.Project)
                .Where(w => !w.Project.IsDeleted);

            if (!isAdmin)
            {
                workItemsQuery = workItemsQuery.Where(w =>
                    w.AssigneeId == userId ||
                    w.CreatedById == userId ||
                    w.Project.OwnerId == userId ||
                    w.Project.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var workItems = await workItemsQuery.ToListAsync();

            // Team members count
            var teamMembersQuery = _context.TeamMembers
                .Include(tm => tm.Project)
                .Where(tm => tm.IsActive);

            if (!isAdmin)
            {
                teamMembersQuery = teamMembersQuery.Where(tm =>
                    tm.Project.OwnerId == userId ||
                    tm.UserId == userId);
            }

            var teamMembers = await teamMembersQuery.Select(tm => tm.UserId).Distinct().ToListAsync();

            var dashboard = new DashboardDto
            {
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active),
                CompletedProjects = projects.Count(p => p.Status == ProjectStatus.Completed),
                OnHoldProjects = projects.Count(p => p.Status == ProjectStatus.OnHold),

                TotalWorkItems = workItems.Count,
                CompletedWorkItems = workItems.Count(w => w.Status == WorkItemStatus.Done),
                InProgressWorkItems = workItems.Count(w => w.Status == WorkItemStatus.InProgress),
                ToDoWorkItems = workItems.Count(w => w.Status == WorkItemStatus.ToDo),
                BlockedWorkItems = workItems.Count(w => w.Status == WorkItemStatus.Blocked),

                TotalTeamMembers = teamMembers.Count,

                WorkItemsByStatus = new List<WorkItemByStatusDto>
                {
                    new() { Status = "To Do", Count = workItems.Count(w => w.Status == WorkItemStatus.ToDo), Color = "secondary" },
                    new() { Status = "In Progress", Count = workItems.Count(w => w.Status == WorkItemStatus.InProgress), Color = "primary" },
                    new() { Status = "Code Review", Count = workItems.Count(w => w.Status == WorkItemStatus.CodeReview), Color = "info" },
                    new() { Status = "Testing", Count = workItems.Count(w => w.Status == WorkItemStatus.Testing), Color = "warning" },
                    new() { Status = "Done", Count = workItems.Count(w => w.Status == WorkItemStatus.Done), Color = "success" },
                    new() { Status = "Blocked", Count = workItems.Count(w => w.Status == WorkItemStatus.Blocked), Color = "danger" }
                },

                ProjectsByStatus = new List<ProjectByStatusDto>
                {
                    new() { Status = "Active", Count = projects.Count(p => p.Status == ProjectStatus.Active), Color = "success" },
                    new() { Status = "On Hold", Count = projects.Count(p => p.Status == ProjectStatus.OnHold), Color = "warning" },
                    new() { Status = "Completed", Count = projects.Count(p => p.Status == ProjectStatus.Completed), Color = "info" },
                    new() { Status = "Archived", Count = projects.Count(p => p.Status == ProjectStatus.Archived), Color = "secondary" },
                    new() { Status = "Cancelled", Count = projects.Count(p => p.Status == ProjectStatus.Cancelled), Color = "danger" }
                }
            };

            dashboard.RecentActivities = await GetRecentActivitiesAsync(userId, isAdmin, 10);
            dashboard.ProjectProgress = await GetProjectProgressAsync(userId, isAdmin);

            return dashboard;
        }

        public async Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(string userId, bool isAdmin, int count = 10)
        {
            var activities = new List<RecentActivityDto>();

            // Get recent projects
            var projectsQuery = _context.Projects
                .Include(p => p.Owner)
                .Where(p => !p.IsDeleted);

            if (!isAdmin)
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
                    UserName = p.Owner.FullName,
                    CreatedAt = p.CreatedAt,
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    Icon = "fa-project-diagram",
                    Color = "success"
                })
                .ToListAsync();

            activities.AddRange(recentProjects);

            // Get recent work items
            var workItemsQuery = _context.WorkItems
                .Include(w => w.Project)
                .Include(w => w.CreatedBy)
                .Where(w => !w.Project.IsDeleted);

            if (!isAdmin)
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

            // Get completed work items
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

            // Sort by date and take top count
            return activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToList();
        }

        public async Task<IEnumerable<ProjectProgressDto>> GetProjectProgressAsync(string userId, bool isAdmin)
        {
            var projectsQuery = _context.Projects
                .Include(p => p.WorkItems)
                .Where(p => !p.IsDeleted && p.Status == ProjectStatus.Active);

            if (!isAdmin)
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.OwnerId == userId ||
                    p.TeamMembers.Any(tm => tm.UserId == userId));
            }

            var projects = await projectsQuery.ToListAsync();

            return projects.Select(p => new ProjectProgressDto
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
            .ToList();
        }
    }
}