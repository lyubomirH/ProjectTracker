using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ProjectTracker.Data;
using ProjectTracker.Data.Entities;
using ProjectTracker.Data.Enums;
using ProjectTracker.Services.Services;

namespace ProjectTracker.Tests.Services
{
    [TestFixture]
    public class TeamServiceTests
    {
        private ApplicationDbContext _context;
        private TeamService _teamService;
        private string _testUserId;
        private int _testProjectId;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;

        [SetUp]
        public void SetUp()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Create Mock UserManager
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, null);

            // Setup UserManager methods
            _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _context.Users.Find(id));

            _mockUserManager.Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser user, string role) => false);

            // Create TeamService with mocked UserManager
            _teamService = new TeamService(_context, _mockUserManager.Object);

            // Seed test data
            SeedTestData();
            _testProjectId = 1;
        }

        private void SeedTestData()
        {
            _testUserId = "test-user-id-123";

            var testUser = new ApplicationUser
            {
                Id = _testUserId,
                UserName = "test@test.com",
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(testUser);

            var testProject = new Project
            {
                Id = 1,
                Name = "Test Project",
                Description = "Test Description",
                StartDate = DateTime.UtcNow,
                Status = ProjectStatus.Active,
                OwnerId = _testUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(testProject);

            // Do NOT add owner as team member - let the service handle it
            // The service will add the owner automatically with role "Owner"

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task GetTeamMembersAsync_ShouldReturnTeamMembers()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "member-to-add",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var memberUser = new ApplicationUser
            {
                Id = "member-to-add",
                UserName = "member@test.com",
                Email = "member@test.com",
                FirstName = "Member",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(memberUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetTeamMembersAsync(_testProjectId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2)); // Owner + team member
        }

        [Test]
        public async Task GetTeamMembersAsync_ShouldIncludeProjectOwner()
        {
            // Act
            var result = await _teamService.GetTeamMembersAsync(_testProjectId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1)); // Only the owner (added by service)
            Assert.That(result.First().UserId, Is.EqualTo(_testUserId));
            // Owner role is "Owner" from the service, not "ProjectManager"
            Assert.That(result.First().Role, Is.EqualTo("Owner"));
        }

        [Test]
        public async Task GetTeamMemberAsync_ShouldReturnTeamMember_WhenExists()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "member-to-add",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var memberUser = new ApplicationUser
            {
                Id = "member-to-add",
                UserName = "member@test.com",
                Email = "member@test.com",
                FirstName = "Member",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(memberUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetTeamMemberAsync(_testProjectId, "member-to-add");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UserId, Is.EqualTo("member-to-add"));
            Assert.That(result.Role, Is.EqualTo("Developer"));
        }

        [Test]
        public async Task GetTeamMemberAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _teamService.GetTeamMemberAsync(_testProjectId, "non-existent-user");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task IsUserTeamMemberAsync_ShouldReturnTrue_WhenUserIsMember()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "member-to-add",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var memberUser = new ApplicationUser
            {
                Id = "member-to-add",
                UserName = "member@test.com",
                Email = "member@test.com",
                FirstName = "Member",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(memberUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.IsUserTeamMemberAsync(_testProjectId, "member-to-add");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsUserTeamMemberAsync_ShouldReturnFalse_WhenUserIsNotMember()
        {
            // Act
            var result = await _teamService.IsUserTeamMemberAsync(_testProjectId, "non-existent-user");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsUserTeamMemberAsync_ShouldReturnFalse_WhenUserIsOwnerButNotInTeam()
        {
            // Act - Owner is not in TeamMembers table
            var result = await _teamService.IsUserTeamMemberAsync(_testProjectId, _testUserId);

            // Assert - Owner is not considered a "team member" unless explicitly added
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task RemoveTeamMemberAsync_ShouldSetInactive()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "member-to-remove",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var memberUser = new ApplicationUser
            {
                Id = "member-to-remove",
                UserName = "member@test.com",
                Email = "member@test.com",
                FirstName = "Member",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(memberUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.RemoveTeamMemberAsync(_testProjectId, "member-to-remove", "admin-user");

            // Assert
            Assert.That(result, Is.True);

            var removedMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == _testProjectId && tm.UserId == "member-to-remove");
            Assert.That(removedMember, Is.Not.Null);
            Assert.That(removedMember!.IsActive, Is.False);
        }

        [Test]
        public async Task RemoveTeamMemberAsync_ShouldReturnFalse_WhenUserIsOwner()
        {
            // Act - Try to remove the project owner
            var result = await _teamService.RemoveTeamMemberAsync(_testProjectId, _testUserId, "admin-user");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task UpdateTeamMemberRoleAsync_ShouldUpdateRole()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "member-to-update",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var memberUser = new ApplicationUser
            {
                Id = "member-to-update",
                UserName = "member@test.com",
                Email = "member@test.com",
                FirstName = "Member",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(memberUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.UpdateTeamMemberRoleAsync(_testProjectId, "member-to-update", "ProjectManager", "admin-user");

            // Assert
            Assert.That(result, Is.True);

            var updatedMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.ProjectId == _testProjectId && tm.UserId == "member-to-update");
            Assert.That(updatedMember!.Role, Is.EqualTo(TeamRole.ProjectManager));
        }

        [Test]
        public async Task UpdateTeamMemberRoleAsync_ShouldReturnFalse_WhenMemberNotFound()
        {
            // Act
            var result = await _teamService.UpdateTeamMemberRoleAsync(_testProjectId, "non-existent-user", "ProjectManager", "admin-user");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsUserProjectManagerAsync_ShouldReturnTrue_WhenUserIsProjectManager()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "pm-user",
                Role = TeamRole.ProjectManager,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var pmUser = new ApplicationUser
            {
                Id = "pm-user",
                UserName = "pm@test.com",
                Email = "pm@test.com",
                FirstName = "PM",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(pmUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.IsUserProjectManagerAsync(_testProjectId, "pm-user");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsUserProjectManagerAsync_ShouldReturnFalse_WhenUserIsNotProjectManager()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "dev-user",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var devUser = new ApplicationUser
            {
                Id = "dev-user",
                UserName = "dev@test.com",
                Email = "dev@test.com",
                FirstName = "Dev",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(devUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.IsUserProjectManagerAsync(_testProjectId, "dev-user");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetAvailableUsersForProjectAsync_ShouldNotReturnExistingMembers()
        {
            // Arrange
            var existingMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "existing-user",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var existingUser = new ApplicationUser
            {
                Id = "existing-user",
                UserName = "existing@test.com",
                Email = "existing@test.com",
                FirstName = "Existing",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(existingUser);
            _context.TeamMembers.Add(existingMember);

            var newUser = new ApplicationUser
            {
                Id = "new-user-id",
                UserName = "new@test.com",
                Email = "new@test.com",
                FirstName = "New",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetAvailableUsersForProjectAsync(_testProjectId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().Id, Is.EqualTo("new-user-id"));
        }

        [Test]
        public async Task GetTeamMembersForDropdownAsync_ShouldIncludeOwnerAndTeamMembers()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = "another-user-id",
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            var anotherUser = new ApplicationUser
            {
                Id = "another-user-id",
                UserName = "another@test.com",
                Email = "another@test.com",
                FirstName = "Another",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(anotherUser);
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetTeamMembersForDropdownAsync(_testProjectId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2)); // Owner + team member
            Assert.That(result.Any(m => m.Role == "Owner"));
            Assert.That(result.Any(m => m.Role == "Developer"));
        }

        [Test]
        public async Task GetUserProjectsAsync_ShouldReturnUserProjects()
        {
            // Arrange
            var teamMember = new TeamMember
            {
                ProjectId = _testProjectId,
                UserId = _testUserId,
                Role = TeamRole.Developer,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.GetUserProjectsAsync(_testUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().ProjectId, Is.EqualTo(_testProjectId));
        }
    }
}