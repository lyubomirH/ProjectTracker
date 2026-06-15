using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Services;
using ProjectTracker.Tests.Helpers;

namespace ProjectTracker.Tests.Services  
{
    [TestFixture]
    public class WorkItemServiceTests
    {
        private ApplicationDbContext _context;
        private WorkItemService _workItemService;
        private string _testUserId;

        [SetUp]
        public void SetUp()
        {
            _context = TestDbContextFactory.CreateInMemoryDbContext();
            _workItemService = new WorkItemService(_context);
            _testUserId = "test-user-id";
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task AssignWorkItemAsync_ShouldAssignUser_WhenUserHasPermission()
        {
            // Arrange
            var assigneeId = "assignee-user-id";
            var isAdmin = false;

            // Create assignee user
            var assigneeUser = new ApplicationUser
            {
                Id = assigneeId,
                UserName = "assignee@test.com",
                Email = "assignee@test.com",
                FirstName = "Assignee",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(assigneeUser);

            // Add user as team member
            var teamMember = new TeamMember
            {
                ProjectId = 1,
                UserId = assigneeId,
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _workItemService.AssignWorkItemAsync(1, assigneeId, _testUserId, isAdmin);

            // Assert
            Assert.That(result, Is.True);

            var updatedItem = await _context.WorkItems.FindAsync(1);
            Assert.That(updatedItem, Is.Not.Null);
            Assert.That(updatedItem!.AssigneeId, Is.EqualTo(assigneeId));
        }
    }
}