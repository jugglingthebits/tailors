using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Tweed.Data;
using Tweed.Data.Entities;
using Tweed.Web.Helper;
using Tweed.Web.Views.Profile;
using Tweed.Web.Views.Shared;

namespace Tweed.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ITweedUserQueries _tweedUserQueries;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly ITweedQueries _tweedQueries;
    private readonly UserManager<TweedIdentityUser> _userManager;

    public ProfileController(ITweedQueries tweedQueries, UserManager<TweedIdentityUser> userManager,
        ITweedUserQueries tweedUserQueries, IViewModelFactory viewModelFactory)
    {
        _tweedQueries = tweedQueries;
        _userManager = userManager;
        _tweedUserQueries = tweedUserQueries;
        _viewModelFactory = viewModelFactory;
    }

    public async Task<IActionResult> Index(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var userTweeds = await _tweedQueries.GetTweedsForUser(userId);
        var currentIdentityUser = await _userManager.GetUserAsync(User);
        var currentTweeUser = await _tweedUserQueries.FindByIdentityUserId(currentIdentityUser.Id!);

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
            currentTweeUser.Follows.Any(f => f.LeaderId == user.Id),
            await _tweedUserQueries.GetFollowerCount(userId)
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
        await _tweedUserQueries.AddFollower(userId, currentUserId, now);

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

        await _tweedUserQueries.RemoveFollower(userId, currentUserId);

        return RedirectToAction("Index", new
        {
            userId
        });
    }
}
