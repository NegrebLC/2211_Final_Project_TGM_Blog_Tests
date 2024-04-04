using _2211_Final_Project_TGM_Blog.Controllers.Forum;
using _2211_Final_Project_TGM_Blog.Data;
using _2211_Final_Project_TGM_Blog.Models.Forum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace _2211_Final_Project_TGM_Blog.Tests.ForumTests
{
    public class CategoryControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CategoryController>> _loggerMock;
        private readonly DefaultHttpContext _httpContext;

        public CategoryControllerTests()
        {
            // Setup DbContext, Logger, and HttpContext for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<CategoryController>>();

            _httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "adminUserId"),
                    new Claim(ClaimTypes.Role, "Admin"),
                }, "TestAuthType"))
            };
        }

        // Helper method to seed the database
        private async Task SeedDatabaseAsync()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Test Category 1", Description = "A test category" },
                // Add more categories as needed
            };

            await _context.Categories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task CategoryController_Index_ReturnsViewResultWithAllCategories()
        {
            // Arrange
            await SeedDatabaseAsync();
            var controller = new CategoryController(_context, _loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Category>>(viewResult.Model);
            Assert.Equal(1, model.Count());
        }

        [Fact]
        public async Task CategoryController_Details_ValidId_ReturnsViewResultWithCategory()
        {
            // Arrange
            await SeedDatabaseAsync();
            var controller = new CategoryController(_context, _loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
            var validId = 1;

            // Act
            var result = await controller.Details(validId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CategoryDetailsViewModel>(viewResult.Model);
            Assert.Equal(validId, model.Category.Id);
        }

        [Fact]
        public async Task CategoryController_Details_InvalidId_ReturnsNotFound()
        {
            // Arrange
            await SeedDatabaseAsync();
            var controller = new CategoryController(_context, _loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
            var invalidId = 999;

            // Act
            var result = await controller.Details(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CategoryController_Create_ValidCategory_RedirectsToIndex()
        {
            // Arrange
            var category = new Category { Name = "New Category", Description = "New Category Description" };
            var controller = new CategoryController(_context, _loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
            controller.ModelState.Clear();

            // Act
            var result = await controller.Create(category);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CategoryController.Index), redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task CategoryController_Create_InvalidCategory_ReturnsViewWithCategory()
        {
            // Arrange
            var category = new Category { Name = "", Description = "" }; // Invalid due to missing name and description
            var controller = new CategoryController(_context, _loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
            controller.ModelState.AddModelError("Name", "The Name field is required.");

            // Act
            var result = await controller.Create(category);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(category, viewResult.Model); // The same category should be returned to the view for correction
        }

        [Fact]
        public async Task CategoryController_DeleteConfirmed_ExistingCategory_RedirectsToIndex()
        {
            // Arrange
            var category = new Category { Id = 1, Name = "Category to Delete", Description = "Description", Threads = new List<ForumThread>() };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            var controller = new CategoryController(_context, _loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };

            // Act
            var result = await controller.DeleteConfirmed(category.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CategoryController.Index), redirectToActionResult.ActionName);
            var deletedCategory = await _context.Categories.FindAsync(category.Id);
            Assert.Null(deletedCategory);
        }

        [Fact]
        public async Task CategoryController_DeleteConfirmed_NonExistentCategory_ReturnsNotFound()
        {
            // Arrange
            var nonExistentCategoryId = 999; // ID not present in the database
            var controller = new CategoryController(_context, _loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };

            // Act
            var result = await controller.DeleteConfirmed(nonExistentCategoryId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}