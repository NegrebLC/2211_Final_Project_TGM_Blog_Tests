﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _2211_Final_Project_TGM_Blog.Data;
using _2211_Final_Project_TGM_Blog.Models.Forum;
using _2211_Final_Project_TGM_Blog.Controllers.Forum;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace _2211_Final_Project_TGM_Blog.Tests.ForumTests
{
    public class ForumThreadControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ForumThreadController>> _loggerMock;

        public ForumThreadControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            _loggerMock = new Mock<ILogger<ForumThreadController>>();
        }

        private ForumThreadController CreateController()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "userId"),
                new Claim(ClaimTypes.Role, "Admin"),
            }, "TestAuthentication"));

            var controller = new ForumThreadController(_context, _loggerMock.Object);
            controller.ControllerContext = new ControllerContext() { HttpContext = httpContext };
            return controller;
        }

        [Fact]
        public async Task ForumThreadController_Details_ValidId_ReturnsViewResultWithThread()
        {
            // Arrange
            var forumThread = new ForumThread { Id = 1, Title = "Test Thread", CategoryId = 1, Posts = new List<Post>() };
            await _context.ForumThreads.AddAsync(forumThread);
            await _context.SaveChangesAsync();
            var controller = new ForumThreadController(_context, _loggerMock.Object);

            // Act
            var result = await controller.Details(forumThread.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ForumThread>(viewResult.Model);
            Assert.Equal(forumThread.Id, model.Id);
        }

        [Fact]
        public async Task ForumThreadController_Details_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var controller = new ForumThreadController(_context, _loggerMock.Object);

            // Act
            var result = await controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void ForumThreadController_Create_ReturnsViewWithCategoryId()
        {
            // Arrange
            var controller = new ForumThreadController(_context, _loggerMock.Object);
            var categoryId = 1;

            // Act
            var result = controller.Create(categoryId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(categoryId, controller.ViewBag.CategoryId);
        }

        [Fact]
        public async Task ForumThreadController_Create_ValidThread_RedirectsToCategoryDetails()
        {
            // Arrange
            var forumThread = new ForumThread { Title = "New Thread", CategoryId = 1 };
            var controller = new ForumThreadController(_context, _loggerMock.Object);

            // Act
            var result = await controller.Create(forumThread);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectToActionResult.ActionName);
            Assert.Equal("Category", redirectToActionResult.ControllerName);
            Assert.Equal(forumThread.CategoryId, redirectToActionResult.RouteValues["id"]);
        }

        [Fact]
        public async Task ForumThreadController_Create_InvalidThread_ReturnsViewWithThread()
        {
            // Arrange
            var controller = new ForumThreadController(_context, _loggerMock.Object);
            controller.ModelState.AddModelError("Title", "Required");
            var forumThread = new ForumThread { CategoryId = 1 };

            // Act
            var result = await controller.Create(forumThread);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(forumThread, viewResult.Model);
        }

        [Fact]
        public async Task ForumThreadController_DeleteConfirmed_ExistingThread_RedirectsToCategoryDetails()
        {
            // Arrange
            var forumThread = new ForumThread { Id = 1, Title = "Thread to Delete", CategoryId = 1, Posts = new List<Post>() };
            await _context.ForumThreads.AddAsync(forumThread);
            await _context.SaveChangesAsync();
            var controller = new ForumThreadController(_context, _loggerMock.Object);

            // Act
            var result = await controller.DeleteConfirmed(forumThread.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectToActionResult.ActionName);
            Assert.Equal("Category", redirectToActionResult.ControllerName);
            Assert.Equal(forumThread.CategoryId, redirectToActionResult.RouteValues["id"]);
        }

        [Fact]
        public async Task ForumThreadController_DeleteConfirmed_NonExistentThread_ReturnsNotFound()
        {
            // Arrange
            var controller = new ForumThreadController(_context, _loggerMock.Object);

            // Act
            var result = await controller.DeleteConfirmed(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}