using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using _2211_Final_Project_TGM_Blog.Controllers.Blog;
using _2211_Final_Project_TGM_Blog.Data;
using _2211_Final_Project_TGM_Blog.Models.Blog;
using _2211_Final_Project_TGM_Blog.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace _2211_Final_Project_TGM_Blog_Tests.BlogTests
{
    public class BlogControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<IdentityUser>> _userManager;
        private readonly Mock<ILogger<LikeService>> _likeServiceLogger;
        private readonly LikeService _likeService;
        private readonly Mock<ILogger<BlogController>> _logger;
        private readonly Mock<IWebHostEnvironment> _hostingEnvironment;
        private readonly DefaultHttpContext _httpContext;

        public BlogControllerTests()
        {
            // Setup for the DbContext, Logger, and HttpContext for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            var userStoreMock = new Mock<IUserStore<IdentityUser>>();
            _userManager = new Mock<UserManager<IdentityUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _logger = new Mock<ILogger<BlogController>>();

            _likeServiceLogger = new ();
            _likeService = new LikeService(_context, _likeServiceLogger.Object);
            _hostingEnvironment = new Mock<IWebHostEnvironment>();

            _httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "devUserId"),
                    new Claim(ClaimTypes.Role, "Dev"),
                }, "TestAuthType"))
            };
        }

        // Helper method to seed the database with data
        private async Task SeedDatabaseAsync()
        {
            var blogs = new List<BlogPost>
            {
                new () { Id = 1, Title = "Test 1", Content = "Content1", UserId = "devUserId" },
                new () { Id = 2, Title = "Test 2", Content = "Content2", UserId = "devUserId" }
            };
            var likes = new List<Like>
            {
                new () { Id = 1, BlogPostId = 1, UserId = "user1", BlogPost = blogs[0]},
                new () { Id = 2, BlogPostId = 2, UserId = "user1", BlogPost = blogs[1]},
                new () { Id = 3, BlogPostId = 1, UserId = "user2", BlogPost = blogs[0]}
            };

            await _context.BlogPosts.AddRangeAsync(blogs);
            await _context.Likes.AddRangeAsync(likes);
            await _context.SaveChangesAsync();
        }

        // Testing the index method
        [Fact]
        public async Task BlogPostController_Index_ReturnsViewResultCorrectly()
        {
            // Arrange
            await SeedDatabaseAsync();
            var blogController = new BlogController(
                _context,
                _userManager.Object,
                _logger.Object,
                _likeService,
                _hostingEnvironment.Object);

            // Act
            var result = blogController.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<BlogPost>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }
    }
}
