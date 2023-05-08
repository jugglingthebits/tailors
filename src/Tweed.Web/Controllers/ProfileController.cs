using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Tweed.Domain;
using Tweed.Domain.Model;
using Tweed.Web.Helper;
using Tweed.Web.Views.Profile;
using Tweed.Web.Views.Shared;

namespace Tweed.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IAppUserFollowsService _appUserFollowsService;
    private readonly ITweedService _tweedService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IViewModelFactory _viewModelFactory;

    public ProfileController(ITweedService tweedService, UserManager<AppUser> userManager,
        IViewModelFactory viewModelFactory, IAppUserFollowsService appUserFollowsService)
    {
        _tweedService = tweedService;
        _userManager = userManager;
        _viewModelFactory = viewModelFactory;
        _appUserFollowsService = appUserFollowsService;
    }

    public async Task<IActionResult> Index(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var userTweeds = await _tweedService.GetTweedsForUser(userId);

        var currentUserId = _userManager.GetUserId(User);
        var currentUserFollows = await _appUserFollowsService.GetFollows(currentUserId!);

        List<TweedViewModel> tweedViewModels = new();
        foreach (var tweed in userTweeds)
        {
            var tweedViewModel = await _viewModelFactory.BuildTweedViewModel(tweed);
            tweedViewModels.Add(tweedViewModel);
        }

        var viewModel = new IndexViewModel(
            userId,
            user.UserName,
            tweedViewModels,
            currentUserFollows.Any(f => f.LeaderId == user.Id),
            await _appUserFollowsService.GetFollowerCount(userId)
        );

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Follow(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (userId == currentUserId)
            return BadRequest("Following yourself isn't allowed");

        var now = SystemClock.Instance.GetCurrentInstant().InUtc();
        await _appUserFollowsService.AddFollower(userId, currentUserId!, now);

        return RedirectToAction("Index", new
        {
            userId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Unfollow(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);

        await _appUserFollowsService.RemoveFollower(userId, currentUserId!);

        return RedirectToAction("Index", new
        {
            userId
        });
    }
}
