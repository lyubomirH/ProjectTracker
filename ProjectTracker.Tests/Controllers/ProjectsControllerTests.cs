using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ProjectTracker.Services.DTOs;
using ProjectTracker.Services.Interfaces;
using ProjectTracker.Web.Controllers;
using ProjectTracker.Web.ViewModels.Projects;
using System.Security.Claims;

namespace ProjectTracker.Tests.Controllers
{
    [TestFixture]
    public class ProjectsControllerTests
    {
        private Mock<IProjectService> _mockProjectService;
        private Mock<IWorkItemService> _mockWorkItemService;
        private Mock<ITeamService> _mockTeamService;
        private ProjectsController _controller;
        private string _testUserId;

        [SetUp]
        public void Setup()
        {
            _mockProjectService = new Mock<IProjectService>();
            _mockWorkItemService = new Mock<IWorkItemService>();
            _mockTeamService = new Mock<ITeamService>();

            _controller = new ProjectsController(
                _mockProjectService.Object,
                _mockWorkItemService.Object,
                _mockTeamService.Object);

            _testUserId = "test-user-id";

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Test]
        public async Task CreatePost_ShouldRedirectToIndex_WhenModelIsValid()
        {
            // Arrange
            var model = new ProjectFormViewModel
            {
                Name = "New Project",
                Description = "Description",
                StartDate = DateTime.Today,
                Status = "Active"
            };

            var createdProject = new ProjectDto
            {
                Id = 1,
                Name = "New Project"
            };

            _mockProjectService.Setup(x => x.CreateProjectAsync(It.IsAny<CreateProjectDto>(), _testUserId))
                .ReturnsAsync(createdProject);

            // Act
            var result = await _controller.Create(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult!.ActionName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task Delete_ShouldRedirectToIndex_WhenSuccessful()
        {
            // Arrange
            _mockProjectService.Setup(x => x.DeleteProjectAsync(1, _testUserId, false))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult!.ActionName, Is.EqualTo("Index"));
        }
    }
}