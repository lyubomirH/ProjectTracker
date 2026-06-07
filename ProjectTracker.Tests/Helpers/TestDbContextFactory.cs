using Microsoft.EntityFrameworkCore;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;

namespace ProjectTracker.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);

            // Seed test data
            SeedTestData(context);

            return context;
        }

        private static void SeedTestData(ApplicationDbContext context)
        {
            var testUserId = "test-user-id";
            var testUser = new ApplicationUser
            {
                Id = testUserId,
                UserName = "test@test.com",
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(testUser);

            var testProject = new Project
            {
                Id = 1,
                Name = "Test Project",
                Description = "Test Description",
                StartDate = DateTime.UtcNow,
                Status = Data.Enums.ProjectStatus.Active,
                OwnerId = testUserId,
                CreatedAt = DateTime.UtcNow
            };

            context.Projects.Add(testProject);

            var testWorkItem = new WorkItem
            {
                Id = 1,
                Title = "Test Work Item",
                Description = "Test Description",
                Priority = Data.Enums.WorkItemPriority.Medium,
                Status = Data.Enums.WorkItemStatus.ToDo,
                ProjectId = 1,
                CreatedById = testUserId,
                CreatedAt = DateTime.UtcNow
            };

            context.WorkItems.Add(testWorkItem);

            // Add the owner as a team member
            var ownerTeamMember = new TeamMember
            {
                ProjectId = 1,
                UserId = testUserId,
                Role = TeamRole.ProjectManager,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            context.TeamMembers.Add(ownerTeamMember);

            context.SaveChanges();
        }
    }
}