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

            // Увери се, че моковете не са null
            Assert.That(_mockProjectService, Is.Not.Null);
            Assert.That(_mockWorkItemService, Is.Not.Null);
            Assert.That(_mockTeamService, Is.Not.Null);

            _controller = new ProjectsController(
                _mockProjectService.Object,
                _mockWorkItemService.Object,
                _mockTeamService.Object);

            Assert.That(_controller, Is.Not.Null);

            _testUserId = "test-user-id";

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.NameIdentifier, _testUserId),
            }, "TestAuth"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        //[Test]
        //public async Task CreatePost_ShouldRedirectToIndex_WhenModelIsValid()
        //{
        //    // Arrange
        //    var model = new ProjectFormViewModel
        //    {
        //        Name = "New Project",
        //        Description = "Test Description",
        //        StartDate = DateTime.Today,
        //        Status = "Active"
        //    };

        //    _mockProjectService
        //        .Setup(x => x.CreateProjectAsync(It.IsAny<CreateProjectDto>(), It.IsAny<string>()))
        //        .ReturnsAsync(new ProjectDto { Id = 1, Name = "New Project" });

        //    // Act
        //    var result = await _controller.Create(model);

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        //}

        //[Test]
        //public async Task Delete_ShouldRedirectToIndex_WhenSuccessful()
        //{
        //    // Arrange
        //    _mockProjectService
        //        .Setup(x => x.DeleteProjectAsync(1, It.IsAny<string>(), false))
        //        .ReturnsAsync(true);

        //    // Act
        //    var result = await _controller.Delete(1);

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        //}

        [Test]
        public async Task Delete_ShouldReturnError_WhenDeleteFails()
        {
            // Arrange
            _mockProjectService
                .Setup(x => x.DeleteProjectAsync(1, _testUserId, false))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());

            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("Error404"));
        }
    }
}