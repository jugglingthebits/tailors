using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Tailors.Domain.ThreadAggregate;
using Tailors.Domain.TweedAggregate;
using Tailors.Domain.UserAggregate;
using Tailors.Domain.UserLikesAggregate;
using Tailors.Web.Test.TestHelper;
using Tailors.Web.Features.Shared;
using Tailors.Web.Features.Tweed;
using Tailors.Web.Helper;
using Xunit;

namespace Tailors.Web.Test.Controllers;

public class TweedControllerTest
{
    private static readonly DateTime FixedDateTime = new DateTime(2022, 11, 18, 15, 20, 0);
    private readonly Mock<ICreateTweedUseCase> _createTweedUseCaseMock = new();
    private readonly ClaimsPrincipal _currentUserPrincipal = ControllerTestHelper.BuildPrincipal();
    private readonly Mock<ILikeTweedUseCase> _likeTweedUseCaseMock = new();
    private readonly Mock<INotificationManager> _notificationManagerMock = new();
    private readonly Mock<IThreadOfTweedsUseCase> _showThreadUseCaseMock = new();
    private readonly TweedController _tweedController;
    private readonly Mock<ITweedRepository> _tweedRepositoryMock = new();

    private readonly Mock<ITweedViewModelFactory> _tweedViewModelFactoryMock = new();

    private readonly Mock<UserManager<AppUser>> _userManagerMock =
        UserManagerMockHelper.MockUserManager<AppUser>();

    public TweedControllerTest()
    {
        _userManagerMock.Setup(u => u.GetUserId(_currentUserPrincipal)).Returns("currentUser");
        _createTweedUseCaseMock.Setup(t =>
                t.CreateRootTweed(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime>()))
            .ReturnsAsync(new TailorsTweed(id: "tweedId", text: string.Empty, createdAt: FixedDateTime, authorId: "authorId"));
        _createTweedUseCaseMock.Setup(t => t.CreateReplyTweed(It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>())).ReturnsAsync(new TailorsTweed(id: "tweedId", text: string.Empty, createdAt: FixedDateTime, authorId: "authorId"));
        _showThreadUseCaseMock
            .Setup(t => t.GetThreadTweedsForTweed(It.IsAny<string>()))
            .ReturnsAsync(new List<TailorsTweed>());
        _tweedController = new TweedController(_tweedRepositoryMock.Object,
            _userManagerMock.Object, _tweedViewModelFactoryMock.Object)
        {
            ControllerContext = ControllerTestHelper.BuildControllerContext(_currentUserPrincipal),
            Url = new Mock<IUrlHelper>().Object
        };
    }

    [Fact]
    public void RequiresAuthorization()
    {
        var authorizeAttributeValue =
            Attribute.GetCustomAttribute(typeof(TweedController), typeof(AuthorizeAttribute));
        Assert.NotNull(authorizeAttributeValue);
    }

    [Fact]
    public async Task ShowThreadForTweed_ShouldReturnViewResult()
    {
        var result =
            await _tweedController.ShowThreadForTweed("tweedId", _showThreadUseCaseMock.Object);

        Assert.IsType<ViewResult>(result);
        var resultAsView = (ViewResult)result;
        Assert.IsType<ShowThreadForTweedViewModel>(resultAsView.Model);
    }

    [Fact]
    public async Task ShowThreadForTweed_ShouldReturnTweeds()
    {
        var rootTweed = new TailorsTweed(id: "tweedId", text: string.Empty, createdAt: FixedDateTime, authorId: "authorId");
        var tweeds = new List<TailorsTweed>
        {
            rootTweed
        };
        _showThreadUseCaseMock
            .Setup(t => t.GetThreadTweedsForTweed(rootTweed.Id!)).ReturnsAsync(tweeds);
        _tweedViewModelFactoryMock.Setup(v => v.Create(tweeds, It.IsAny<string>())).ReturnsAsync(
            new List<TweedViewModel>
            {
                new()
                {
                    Id = "tweedId"
                }
            });

        var result =
            await _tweedController.ShowThreadForTweed("tweedId", _showThreadUseCaseMock.Object);

        var resultViewModel = (ShowThreadForTweedViewModel)((ViewResult)result).Model!;
        Assert.Equal(resultViewModel.Tweeds[0].Id, rootTweed.Id);
    }

    [Fact]
    public async Task ShowThreadForTweed_ShouldSetParentTweedId()
    {
        var result = await _tweedController.ShowThreadForTweed(HttpUtility.UrlEncode("tweeds/1"),
            _showThreadUseCaseMock.Object);

        var resultViewModel = (ShowThreadForTweedViewModel)((ViewResult)result).Model!;
        Assert.Equal("tweeds/1", resultViewModel.CreateReplyTweed.ParentTweedId);
    }

    [Fact]
    public async Task Create_ShouldReturnRedirect()
    {
        CreateTweedViewModel viewModel = new()
        {
            Text = "test"
        };
        var result = await _tweedController.Create(viewModel, _createTweedUseCaseMock.Object,
            _notificationManagerMock.Object);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Create_ShouldSaveTweed()
    {
        CreateTweedViewModel viewModel = new()
        {
            Text = "test"
        };
        await _tweedController.Create(viewModel, _createTweedUseCaseMock.Object,
            _notificationManagerMock.Object);

        _createTweedUseCaseMock.Verify(t =>
            t.CreateRootTweed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()));
    }


    [Fact]
    public async Task Create_ShouldSetSuccessMessage()
    {
        CreateTweedViewModel viewModel = new()
        {
            Text = "test"
        };
        await _tweedController.Create(viewModel, _createTweedUseCaseMock.Object,
            _notificationManagerMock.Object);

        _notificationManagerMock.Verify(n => n.AppendSuccess("Tweed Posted"));
    }

    [Fact]
    public async Task CreateReply_ShouldReturnRedirect()
    {
        _tweedRepositoryMock.Setup(t => t.GetById("parentTweedId"))
            .ReturnsAsync(new TailorsTweed(text: string.Empty, createdAt: FixedDateTime, authorId: "authorId"));
        _tweedRepositoryMock.Setup(t => t.GetById("rootTweedId"))
            .ReturnsAsync(new TailorsTweed(text: string.Empty, createdAt: FixedDateTime, authorId: "authorId"));

        CreateReplyTweedViewModel viewModel = new()
        {
            Text = "test",
            ParentTweedId = "parentTweedId"
        };
        var result = await _tweedController.CreateReply(viewModel, _createTweedUseCaseMock.Object,
            _notificationManagerMock.Object);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task CreateReply_ShouldSaveReplyTweed()
    {
        _tweedRepositoryMock.Setup(t => t.GetById("parentTweedId"))
            .ReturnsAsync(new TailorsTweed(text: string.Empty, createdAt: FixedDateTime, authorId: "authorId"));
        _tweedRepositoryMock.Setup(t => t.GetById("rootTweedId"))
            .ReturnsAsync(new TailorsTweed(text: string.Empty, createdAt: FixedDateTime, authorId: "authorId"));

        CreateReplyTweedViewModel viewModel = new()
        {
            Text = "text",
            ParentTweedId = "parentTweedId"
        };
        await _tweedController.CreateReply(viewModel, _createTweedUseCaseMock.Object,
            _notificationManagerMock.Object);

        _createTweedUseCaseMock.Verify(t => t.CreateReplyTweed(It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<string>()));
    }

    [Fact]
    public async Task CreateReply_ShouldSetSuccessMessage()
    {
        _tweedRepositoryMock.Setup(t => t.GetById("parentTweedId"))
            .ReturnsAsync(new TailorsTweed(text: string.Empty, createdAt: FixedDateTime, authorId: "authorId"));
        _tweedRepositoryMock.Setup(t => t.GetById("rootTweedId"))
            .ReturnsAsync(new TailorsTweed(text: string.Empty, createdAt: FixedDateTime, authorId: "authorId"));

        CreateReplyTweedViewModel viewModel = new()
        {
            Text = "test",
            ParentTweedId = "parentTweedId"
        };
        await _tweedController.CreateReply(viewModel, _createTweedUseCaseMock.Object,
            _notificationManagerMock.Object);

        _notificationManagerMock.Verify(n => n.AppendSuccess("Reply Posted"));
    }

    [Fact]
    public async Task CreateReply_ShouldReturnBadRequest_WhenParentTweedIdIsMissing()
    {
        CreateReplyTweedViewModel viewModel = new()
        {
            Text = "test"
        };
        _createTweedUseCaseMock
            .Setup(m => m.CreateReplyTweed(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>())).ReturnsAsync(new TweedError("Tweed error"));

        var result = await _tweedController.CreateReply(viewModel, _createTweedUseCaseMock.Object,
            _notificationManagerMock.Object);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task CreateReply_ShouldReturnBadRequest_WhenParentTweedDoesntExist()
    {
        CreateReplyTweedViewModel viewModel = new()
        {
            Text = "test",
            ParentTweedId = "nonExistingTweed"
        };
        _createTweedUseCaseMock
            .Setup(m => m.CreateReplyTweed(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>())).ReturnsAsync(new TweedError("Tweed error"));
        var result = await _tweedController.CreateReply(viewModel, _createTweedUseCaseMock.Object,
            _notificationManagerMock.Object);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Like_ShouldIncreaseLikes()
    {
        TailorsTweed tweed = new(authorId: "author", text: string.Empty, createdAt: FixedDateTime);
        _tweedRepositoryMock.Setup(t => t.GetById("123")).ReturnsAsync(tweed);

        await _tweedController.Like("123", false, _likeTweedUseCaseMock.Object);

        _likeTweedUseCaseMock.Verify(u =>
            u.AddLike("123", "currentUser", It.IsAny<DateTime>()));
    }

    [Fact]
    public async Task Like_ShouldReturnPartialView()
    {
        TailorsTweed tweed = new(authorId: "author", text: string.Empty, createdAt: FixedDateTime);
        _tweedRepositoryMock.Setup(t => t.GetById("123")).ReturnsAsync(tweed);
        _userManagerMock.Setup(u => u.FindByIdAsync("author")).ReturnsAsync(new AppUser());

        var result = await _tweedController.Like("123", false, _likeTweedUseCaseMock.Object);

        Assert.IsType<PartialViewResult>(result);
    }

    [Fact]
    public async Task Unlike_ShouldDecreaseLikes()
    {
        TailorsTweed tweed = new(authorId: "author", text: string.Empty, createdAt: FixedDateTime);
        _tweedRepositoryMock.Setup(t => t.GetById("123")).ReturnsAsync(tweed);

        await _tweedController.Unlike("123", false, _likeTweedUseCaseMock.Object);

        _likeTweedUseCaseMock.Verify(u => u.RemoveLike("123", "currentUser"));
    }

    [Fact]
    public async Task Unlike_ShouldReturnPartialView()
    {
        TailorsTweed tweed = new(authorId: "authorId", text: string.Empty, createdAt: FixedDateTime);
        _tweedRepositoryMock.Setup(t => t.GetById("123")).ReturnsAsync(tweed);
        _userManagerMock.Setup(u => u.FindByIdAsync("authorId")).ReturnsAsync(new AppUser());

        var result = await _tweedController.Unlike("123", false, _likeTweedUseCaseMock.Object);

        Assert.IsType<PartialViewResult>(result);
    }
}
