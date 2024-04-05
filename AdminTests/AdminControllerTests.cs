using _2211_Final_Project_TGM_Blog.Controllers.Admin;
using _2211_Final_Project_TGM_Blog.Services;
using _2211_Final_Project_TGM_Blog.Models.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
namespace _2211_Final_Project_TGM_Blog_Tests.AdminTests
{
    public class AdminControllerTests
    {
        private readonly DefaultHttpContext _httpContext;
        private readonly UserAccountService _userAccountService;
        private readonly UserRoleManager _userRoleManager;
        private readonly Mock<ITempDataProvider> _tempDataProviderMock;

        public AdminControllerTests()
        {
            // Setup HttpContext to simulate User Identity
            _httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "userId"),
                }, "TestAuthType"))
            };

            // Mock ITempDataProvider
            _tempDataProviderMock = new Mock<ITempDataProvider>();

            var controller = new AdminController(_userAccountService, _tempDataProviderMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };

            // Mock UserManager<IdentityUser>
            var userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<IdentityUser>>(),
                new IUserValidator<IdentityUser>[0],
                new IPasswordValidator<IdentityUser>[0],
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<IdentityUser>>>()
            );

            // Mock UserManager<IdentityUser> to return a user when FindByNameAsync is called
            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string userName) =>
                {
                    if (userName == "InvalidUsername")
                    {
                        // Return null to simulate the user not found scenario
                        return null;
                    }
                    // Create a mock user for testing purposes
                    var user = new IdentityUser { UserName = userName };
                    return user;
                });

            // Mock RoleManager<IdentityRole>
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                new IRoleValidator<IdentityRole>[0],
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<ILogger<RoleManager<IdentityRole>>>()
            );


            // Mock ILogger<UserRoleManager>
            var loggerMock = new Mock<ILogger<UserRoleManager>>();

            // Create an instance of UserRoleManager with the mocked dependencies
            _userRoleManager = new UserRoleManager(userManagerMock.Object, roleManagerMock.Object, loggerMock.Object);

            // Create an instance of UserAccountService with the mocked dependencies
            _userAccountService = new UserAccountService(userManagerMock.Object, roleManagerMock.Object, _userRoleManager);
        }




        // Testing null Search returns baseview with no details
        [Fact]
        public async Task UserAccounts_NullSearch_ReturnsBaseView()
        {
            // Mock ITempDataProvider
            var tempDataProviderMock = new Mock<ITempDataProvider>();

            var controller = new AdminController(_userAccountService, tempDataProviderMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
            // Act
            var result = await controller.UserAccounts(null) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
        }

        // Testing valid username search returns baseview of UserAccounts.cshtml with _UserDetails.cshtml
        [Fact]
        public async Task UserAccounts_ValidUsernameSearch_ReturnsPartialView()
        {
            // Mock ITempDataProvider
            var tempDataProviderMock = new Mock<ITempDataProvider>();

            var controller = new AdminController(_userAccountService, tempDataProviderMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
            // Act
            var result = await controller.UserAccounts("Admin") as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Model);
        }

        //Testing invalid username search returns baseview of UserAccounts.cshtml with and errormessage
        [Fact]
        public async Task UserAccounts_InvalidUsernameSearch_ReturnsViewWithErrorMessage()
        {
            // Mock ITempDataProvider
            var tempDataProviderMock = new Mock<ITempDataProvider>();

            var controller = new AdminController(_userAccountService, tempDataProviderMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
            // Act
            var result = await controller.UserAccounts("InvalidUsername") as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.TempData["ErrorMessage"]);
        }

        //Testing an invalid model state will redirect with an errormessage
        [Fact]
        public async Task Update_InvalidModelState_ReturnsRedirectWithErrorMessage()
        {
            // Mock ITempDataProvider
            var tempDataProviderMock = new Mock<ITempDataProvider>();
            var controller = new AdminController(_userAccountService, tempDataProviderMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };
            controller.ModelState.AddModelError("PropertyName", "Error message");

            // Act
            var result = await controller.Update(new UserAccounts());

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserAccounts", redirectToActionResult.ActionName);
            var tempData = controller.TempData;
            Assert.NotNull(tempData);
            Assert.True(tempData.ContainsKey("ErrorMessage"));
        }

        //Testing a valid modelstate redirects successfully
        [Fact]
        public async Task Update_SuccessfulUpdate_RedirectsToUserAccountsAction()
        {
            // Arrange
            var userAccountServiceMock = new Mock<UserAccountService>();
            userAccountServiceMock.Setup(x => x.UpdateUserAccountAsync(It.IsAny<UserAccounts>())).ReturnsAsync(true);

            // Mock ITempDataProvider
            var tempDataProviderMock = new Mock<ITempDataProvider>();

            var controller = new AdminController(_userAccountService, tempDataProviderMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };

            // Act
            var result = await controller.Update(new UserAccounts { Search = "search" });

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserAccounts", redirectToActionResult.ActionName);
        }


        //Testing a failed update redirects with an errormessage
        [Fact]
        public async Task Update_FailedUpdate_ReturnsRedirectWithErrorMessage()
        {
            // Arrange
            var userAccountServiceMock = new Mock<UserAccountService>();
            userAccountServiceMock.Setup(x => x.UpdateUserAccountAsync(It.IsAny<UserAccounts>())).ReturnsAsync(false);

            // Mock ITempDataProvider
            var tempDataProviderMock = new Mock<ITempDataProvider>();

            var controller = new AdminController(userAccountServiceMock.Object, tempDataProviderMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext }
            };

            // Act
            var result = await controller.Update(new UserAccounts { Search = "searchQuery" });

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserAccounts", redirectToActionResult.ActionName);

            // Check if TempData contains the expected error message
            var tempData = controller.TempData;
            Assert.NotNull(tempData);
            Assert.True(tempData.ContainsKey("ErrorMessage"));
            Assert.Equal("Unexpected error. Update failed", tempData["ErrorMessage"]);
        }

    }
}
