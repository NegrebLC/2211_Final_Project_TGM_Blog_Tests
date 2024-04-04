using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using _2211_Final_Project_TGM_Blog.Controllers.Forum;
using _2211_Final_Project_TGM_Blog.Data;
using _2211_Final_Project_TGM_Blog.Models.Forum;
using Microsoft.AspNetCore.Hosting;

namespace _2211_Final_Project_TGM_Blog.Tests.ForumTests
{
    public class PostControllerTests
    {
        private readonly Mock<ILogger<PostController>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _hostingEnvironmentMock;
        private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
        private readonly ApplicationDbContext _context;

        public PostControllerTests()
        {
            // Mock setup for ILogger, IWebHostEnvironment, and UserManager
            _loggerMock = new Mock<ILogger<PostController>>();
            _hostingEnvironmentMock = new Mock<IWebHostEnvironment>();
            var store = new Mock<IUserStore<IdentityUser>>();
            _userManagerMock = new Mock<UserManager<IdentityUser>>(store.Object,
                                                                   null,
                                                                   null,
                                                                   null,
                                                                   null,
                                                                   null,
                                                                   null,
                                                                   null,
                                                                   null);

            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _context = new ApplicationDbContext(options);
        }

        private PostController CreateController()
        {
            // Setup HttpContext for User Identity
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
            }, "TestAuthentication"));

            var controller = new PostController(_context, _loggerMock.Object, _hostingEnvironmentMock.Object, _userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return controller;
        }

        [Fact]
        public async Task Create_PostWithValidData_ReturnsRedirectToActionResult()
        {
            // Arrange
            var mockImageFile = new Mock<IFormFile>();
            var fileName = "testImage.png";
            var content = "Fake image content";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockImageFile.Setup(_ => _.OpenReadStream()).Returns(ms);
            mockImageFile.Setup(_ => _.FileName).Returns(fileName);
            mockImageFile.Setup(_ => _.Length).Returns(ms.Length);

            var controller = CreateController();

            var post = new Post
            {
                Content = "Test content",
                ThreadId = 1,
                UserId = "testUserId"
            };

            _userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new IdentityUser { Id = "testUserId", UserName = "testUser" });

            _hostingEnvironmentMock.Setup(env => env.WebRootPath).Returns(Path.GetTempPath());

            // Act
            var result = await controller.Create(post, mockImageFile.Object);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectToActionResult.ActionName);
            Assert.Equal("ForumThread", redirectToActionResult.ControllerName);
            Assert.NotNull(redirectToActionResult.RouteValues["id"]);
            Assert.Equal(post.ThreadId, redirectToActionResult.RouteValues["id"]);
        }

        [Fact]
        public async Task DeleteConfirmed_ExistingPost_ReturnsRedirectToActionResult()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDbForDelete")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                context.Posts.Add(new Post { Id = 1, Content = "Test Content", ThreadId = 1, UserId = "testUserId" });
                await context.SaveChangesAsync();
            }

            using (var context = new ApplicationDbContext(options))
            {
                var controller = new PostController(context, _loggerMock.Object, _hostingEnvironmentMock.Object, _userManagerMock.Object);

                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                }, "TestAuthentication"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };

                // Act
                var result = await controller.DeleteConfirmed(1);

                // Assert
                var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Details", redirectToActionResult.ActionName);
                Assert.Equal("ForumThread", redirectToActionResult.ControllerName);
                Assert.NotNull(redirectToActionResult.RouteValues["id"]);
                Assert.Equal(1, redirectToActionResult.RouteValues["id"]);

                Assert.DoesNotContain(context.Posts, p => p.Id == 1);
            }
        }

        [Fact]
        public async Task DeleteConfirmed_NonExistingPost_ReturnsNotFoundResult()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDbForDeleteNotFound")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var controller = new PostController(context, _loggerMock.Object, _hostingEnvironmentMock.Object, _userManagerMock.Object);

                var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.NameIdentifier, "testUserId"),
                }, "TestAuthentication"));
                controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = userClaims } };

                // Act
                var result = await controller.DeleteConfirmed(999);

                // Assert
                Assert.IsType<NotFoundResult>(result);
            }
        }
    }
}
