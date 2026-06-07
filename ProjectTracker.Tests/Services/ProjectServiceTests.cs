using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.Services;
using ProjectTracker.Tests.Helpers;

namespace ProjectTracker.Tests.Services
{
    [TestFixture]
    public class ProjectServiceTests
    {
        private ApplicationDbContext _context;
        private ProjectService _projectService;
        private string _testUserId;

        [SetUp]
        public void SetUp()
        {
            _context = TestDbContextFactory.CreateInMemoryDbContext();
            _projectService = new ProjectService(_context);
            _testUserId = "test-user-id";
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task IsUserInProjectAsync_ShouldReturnTrue_WhenUserIsOwner()
        {
            // Act
            var result = await _projectService.IsUserInProjectAsync(1, _testUserId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsUserInProjectAsync_ShouldReturnTrue_WhenUserIsTeamMember()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = 1,
                UserId = "team-member-id",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var teamUser = new ApplicationUser
            {
                Id = "team-member-id",
                UserName = "team@test.com",
                Email = "team@test.com",
                FirstName = "Team",
                LastName = "Member",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(teamUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _projectService.IsUserInProjectAsync(1, "team-member-id");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsUserInProjectAsync_ShouldReturnFalse_WhenUserIsNotMember()
        {
            // Act
            var result = await _projectService.IsUserInProjectAsync(1, "non-existent-user");

            // Assert
            Assert.That(result, Is.False);
        }
    }
}