using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;

namespace ProjectTracker.Data.Seed
{
    public static class SampleDataSeeder
    {
        public static async Task SeedSampleDataAsync(ApplicationDbContext context)
        {
            if (!await context.Projects.AnyAsync())
            {
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@tracker.com");
                if (adminUser != null)
                {
                    var projects = new[]
                    {
                        new Project
                        {
                            Name = "E-Commerce Platform",
                            Description = "Build a modern e-commerce platform with React and ASP.NET Core",
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddMonths(3),
                            Status = ProjectStatus.Active,
                            OwnerId = adminUser.Id
                        },
                        new Project
                        {
                            Name = "Mobile Banking App",
                            Description = "Cross-platform mobile banking application for iOS and Android",
                            StartDate = DateTime.UtcNow.AddDays(-30),
                            EndDate = DateTime.UtcNow.AddMonths(4),
                            Status = ProjectStatus.OnHold,
                            OwnerId = adminUser.Id
                        },
                        new Project
                        {
                            Name = "Company Website Redesign",
                            Description = "Redesign the corporate website with new branding and modern UI",
                            StartDate = DateTime.UtcNow.AddDays(-60),
                            EndDate = DateTime.UtcNow.AddDays(30),
                            Status = ProjectStatus.Active,
                            OwnerId = adminUser.Id
                        },
                        new Project
                        {
                            Name = "API Gateway Service",
                            Description = "Build a scalable API gateway for microservices architecture",
                            StartDate = DateTime.UtcNow.AddDays(-15),
                            Status = ProjectStatus.Active,
                            OwnerId = adminUser.Id
                        },
                        new Project
                        {
                            Name = "Data Analytics Dashboard",
                            Description = "Create interactive dashboards for business intelligence",
                            StartDate = DateTime.UtcNow.AddDays(-90),
                            EndDate = DateTime.UtcNow.AddDays(-10),
                            Status = ProjectStatus.Completed,
                            OwnerId = adminUser.Id
                        }
                    };

                    await context.Projects.AddRangeAsync(projects);
                    await context.SaveChangesAsync();

                    var firstProject = projects[0];
                    var workItems = new[]
                    {
                        new WorkItem
                        {
                            Title = "Setup Database Schema",
                            Description = "Design and implement database schema for the e-commerce platform",
                            Priority = WorkItemPriority.High,
                            Status = WorkItemStatus.InProgress,
                            ProjectId = firstProject.Id,
                            CreatedById = adminUser.Id,
                            DueDate = DateTime.UtcNow.AddDays(7),
                            EstimatedHours = 20
                        },
                        new WorkItem
                        {
                            Title = "Implement Authentication",
                            Description = "Add JWT authentication and user management with refresh tokens",
                            Priority = WorkItemPriority.Critical,
                            Status = WorkItemStatus.ToDo,
                            ProjectId = firstProject.Id,
                            CreatedById = adminUser.Id,
                            DueDate = DateTime.UtcNow.AddDays(14),
                            EstimatedHours = 40
                        },
                        new WorkItem
                        {
                            Title = "Create Product Catalog API",
                            Description = "Build REST API for product management with filtering and pagination",
                            Priority = WorkItemPriority.High,
                            Status = WorkItemStatus.ToDo,
                            ProjectId = firstProject.Id,
                            CreatedById = adminUser.Id,
                            DueDate = DateTime.UtcNow.AddDays(10),
                            EstimatedHours = 30
                        },
                        new WorkItem
                        {
                            Title = "Implement Shopping Cart",
                            Description = "Add shopping cart functionality with session management",
                            Priority = WorkItemPriority.Medium,
                            Status = WorkItemStatus.ToDo,
                            ProjectId = firstProject.Id,
                            CreatedById = adminUser.Id,
                            DueDate = DateTime.UtcNow.AddDays(21),
                            EstimatedHours = 35
                        },
                        new WorkItem
                        {
                            Title = "Setup CI/CD Pipeline",
                            Description = "Configure GitHub Actions for automated testing and deployment",
                            Priority = WorkItemPriority.Medium,
                            Status = WorkItemStatus.CodeReview,
                            ProjectId = firstProject.Id,
                            CreatedById = adminUser.Id,
                            DueDate = DateTime.UtcNow.AddDays(5),
                            EstimatedHours = 15
                        }
                    };

                    await context.WorkItems.AddRangeAsync(workItems);
                    await context.SaveChangesAsync();

                    var firstWorkItem = workItems[0];
                    var comments = new[]
                    {
                        new Comment
                        {
                            Content = "Database schema design approved. Starting implementation.",
                            WorkItemId = firstWorkItem.Id,
                            AuthorId = adminUser.Id
                        },
                        new Comment
                        {
                            Content = "Need to add indexing for better performance",
                            WorkItemId = firstWorkItem.Id,
                            AuthorId = adminUser.Id
                        }
                    };

                    await context.Comments.AddRangeAsync(comments);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}