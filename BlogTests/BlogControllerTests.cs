using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using _2211_Final_Project_TGM_Blog.Controllers.Blog;
using _2211_Final_Project_TGM_Blog.Data;
using _2211_Final_Project_TGM_Blog.Models.Blog;
using _2211_Final_Project_TGM_Blog.Services;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly Mock<LikeService> _likeService;
        private readonly Mock<ILogger<BlogController>> _logger;
        private readonly Mock<IWebHostEnvironment> _hostingEnvironment;
        private DefaultHttpContext _httpContext;

        private readonly BlogController _blogController;

        public BlogControllerTests()
        {
            //setting up the DbContext, UserManager, Logger, LikeService, and HttpContext for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            var userStoreMock = new Mock<IUserStore<IdentityUser>>();
            _userManager = new Mock<UserManager<IdentityUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _logger = new Mock<ILogger<BlogController>>();

            _likeServiceLogger = new();
            _likeService = new Mock<LikeService>(_context, _likeServiceLogger.Object);
            _hostingEnvironment = new Mock<IWebHostEnvironment>();

            _blogController = new BlogController(
                _context,
                _userManager.Object,
                _logger.Object,
                _likeService.Object,
                _hostingEnvironment.Object);

            _httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "devUserId"),
                    new Claim(ClaimTypes.Role, "Dev"),
                }, "TestAuthType"))
            };

            _blogController.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new IdentityUser());
        }

        //helper method that seeds the DB
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
                new () { Id = 2, BlogPostId = 2, UserId = "devUserId", BlogPost = blogs[1]},
                new () { Id = 3, BlogPostId = 1, UserId = "user2", BlogPost = blogs[0]}
            };

            await _context.BlogPosts.AddRangeAsync(blogs);
            await _context.Likes.AddRangeAsync(likes);
            await _context.SaveChangesAsync();
        }

        private void NonDevUser()
        {
            var nonDevUserClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "nonDevUserId"),
                new Claim(ClaimTypes.Role, "NonDev"),
            };
            var nonDevUserPrincipal = new ClaimsPrincipal(new ClaimsIdentity(nonDevUserClaims, "TestAuthType"));
            _blogController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = nonDevUserPrincipal }
            };
        }

        //testing the index method
        [Fact]
        public async Task BlogPostController_Index_ReturnsViewResultCorrectly()
        {
            //arrange
            await SeedDatabaseAsync();

            //act
            var result = _blogController.Index();

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<BlogPost>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        //testing that the create view only returns for authorized users
        [Fact]
        public void CreateBlogPost_ReturnsViewResult_ForAuthorizedUser()
        {
            //act
            var result = _blogController.CreateBlogPost();
            //assert
            Assert.IsType<ViewResult>(result);
        }

        //testing an unauthorized user attempting to gain access to the create blog post action
        [Fact]
        public void CreateBlogPost_ReturnsUnauthorized_ForNonAuthorizedUser()
        {
            //arrange
            NonDevUser();

            //act
            var result = _blogController.CreateBlogPost();

            //assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        //testing that the create post correctly redirects a user to the index after posting
        [Fact]
        public async Task CreateBlogPost_Post_ReturnsRedirectToActionResult_ForAuthorizedUser()
        {
            //arrange
            await SeedDatabaseAsync();

            var blogPost = new BlogPost
            {
                Title = "Test Title",
                Content = "Test Content",
                UserId = "devUserId"
            };

            // Act
            var result = await _blogController.CreateBlogPost(blogPost, null);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        //testing handling of an invalid blogpost
        [Fact]
        public async Task CreateBlogPost_InvalidModelState_ReturnsViewResult()
        {
            //arrange
            var invalidBlogPost = new BlogPost
            {
                Title = "BadData"
            };

            //act
            var result = await _blogController.CreateBlogPost(invalidBlogPost, null);

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(invalidBlogPost, viewResult.Model);
        }

        //testing an unauthorized user attempting to gain access to the create blog post action
        [Fact]
        public async Task CreateBlogPost_ReturnsUnauthorized_NonAuthorizedUserCreatingBlogPost()
        {
            //arrange
            var blogPost = new BlogPost
            {
                Title = "Test Title",
                Content = "Test Content",
                UserId = "devUserId"
            };

            NonDevUser();

            //act
            var result = await _blogController.CreateBlogPost(blogPost, null);

            //assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        //testing the delete method to ensure it redirects properly for a registered user
        [Fact]
        public async Task Delete_Post_ReturnsRedirectToActionResultForAuthorizedUser()
        {
            //arrange
            var postIdToDelete = 1;

            //act
            var result = await _blogController.Delete(postIdToDelete);

            //assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        //testing the delete method to ensure non-devs cannot delete
        [Fact]
        public async Task Delete_Post_ReturnsUnauthorized_NonAuthorizedUserDeletingBlogPost()
        {
            //arrange
            var postIdToDelete = 1;

            NonDevUser();

            //act
            var result = await _blogController.Delete(postIdToDelete);

            //assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        //testing that the like method posts correctly for authenticated users
        [Fact]
        public async Task Like_ValidPostId_ReturnsPartialView()
        {
            //arrange
            await SeedDatabaseAsync();

            var postId = 1;

            _likeService.Setup(ls => ls.Like(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<BlogPost>()));
            _likeService.Setup(ls => ls.GetLikes(It.IsAny<int>())).ReturnsAsync(1);
          
            //act
            var result = await _blogController.Like(postId) as PartialViewResult;

            //assert
            Assert.NotNull(result);
            Assert.Equal("_LikeButtonPartial", result.ViewName);
            Assert.IsType<LikeButtonModel>(result.Model);            
        }

        //testing the like so that an exception redirects to the index
        [Fact]
        public async Task Like_ExceptionRedirectsToIndex()
        {
            //arrange
            await SeedDatabaseAsync();

            var postId = 1;

            _likeService.Setup(ls => ls.Like(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<BlogPost>()))
                         .ThrowsAsync(new Exception("Test exception"));

            //act
            var result = await _blogController.Like(postId) as RedirectToActionResult;

            //assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Null(result.ControllerName);
        }

        //testing that the unlike method posts correctly for authenticated users
        [Fact]
        public async Task Unlike_ValidLikeId_ReturnsPartialView()
        {
            //arange
            await SeedDatabaseAsync();
            var likeId = 2;
            var validLike = await _context.Likes.FindAsync(likeId);

            _likeService.Setup(ls => ls.GetLikeById(It.IsAny<int>())).ReturnsAsync(validLike);
            _likeService.Setup(ls => ls.Unlike(It.IsAny<int>())).Returns(Task.CompletedTask);

            //act
            var result = await _blogController.Unlike(likeId) as PartialViewResult;

            //assert
            Assert.NotNull(result);
            Assert.Equal("_LikeButtonPartial", result.ViewName);
            Assert.IsType<LikeButtonModel>(result.Model);
        }

        //testing that the unlike button returns a bad request on an invalid
        [Fact]
        public async Task Unlike_ReturnsBadRequestWhenLikeNotFound()
        {
            //arange
            await SeedDatabaseAsync();
            var likeId = -1;
            _likeService.Setup(ls => ls.GetLikeById(It.IsAny<int>())).ReturnsAsync((Like)null);

            //act
            var result = await _blogController.Unlike(likeId) as BadRequestResult;

            //assert
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }
    }
}
