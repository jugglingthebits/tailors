using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Tweed.Data;
using Tweed.Data.Entities;
using Tweed.Web.Views.Profile;
using Tweed.Web.Views.Shared;

namespace Tweed.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IAppUserQueries _appUserQueries;
    private readonly ITweedQueries _tweedQueries;
    private readonly UserManager<AppUser> _userManager;

    public ProfileController(ITweedQueries tweedQueries, UserManager<AppUser> userManager,
        IAppUserQueries appUserQueries)
    {
        _tweedQueries = tweedQueries;
        _userManager = userManager;
        _appUserQueries = appUserQueries;
    }

    public async Task<IActionResult> Index(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var userTweeds = await _tweedQueries.GetTweedsForUser(userId);
        var currentUser = await _userManager.GetUserAsync(User);

        List<TweedViewModel> tweedViewModels = new();
        foreach (var tweed in userTweeds)
        {
            var author = await _userManager.FindByIdAsync(tweed.AuthorId);
            var tweedViewModel =
                ViewModelFactory.BuildTweedViewModel(tweed, author, currentUser.Id!);
            tweedViewModels.Add(tweedViewModel);
        }

        var viewModel = new IndexViewModel
        {
            UserName = user.UserName,
            Tweeds = tweedViewModels,
            CurrentUserFollows = currentUser.Follows.Any(f => f.LeaderId == user.Id)
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Follow(string leaderId)
    {
        var leader = await _userManager.FindByIdAsync(leaderId);
        if (leader == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var now = SystemClock.Instance.GetCurrentInstant().InUtc();

        await _appUserQueries.AddFollower(leaderId, currentUserId, now);

        return Ok();
    }
}
