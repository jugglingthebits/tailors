using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Tweed.Domain;
using Tweed.Domain.Model;
using Tweed.Web.Helper;
using Tweed.Web.Views.Tweed;

namespace Tweed.Web.Controllers;

[Authorize]
public class TweedController : Controller
{
    private readonly ILikeTweedUseCase _likeTweedUseCase;
    private readonly INotificationManager _notificationManager;
    private readonly IShowThreadUseCase _showThreadUseCase;
    private readonly ITweedRepository _tweedRepository;
    private readonly ITweedViewModelFactory _tweedViewModelFactory;
    private readonly UserManager<User> _userManager;

    public TweedController(ITweedRepository tweedRepository,
        UserManager<User> userManager,
        INotificationManager notificationManager, ILikeTweedUseCase likeTweedUseCase,
        IShowThreadUseCase showThreadUseCase,
        ITweedViewModelFactory tweedViewModelFactory)
    {
        _tweedRepository = tweedRepository;
        _userManager = userManager;
        _notificationManager = notificationManager;
        _likeTweedUseCase = likeTweedUseCase;
        _showThreadUseCase = showThreadUseCase;
        _tweedViewModelFactory = tweedViewModelFactory;
    }

    [HttpGet("Tweed/{tweedId}")]
    public async Task<ActionResult> ShowThreadForTweed(string tweedId)
    {
        var decodedTweedId =
            HttpUtility.UrlDecode(tweedId); // ASP.NET Core doesn't auto-decode parameters

        var threadTweeds = await _showThreadUseCase.GetThreadTweedsForTweed(decodedTweedId);
        if (threadTweeds.IsFailed)
            return NotFound();

        ShowThreadForTweedViewModel viewModel = new()
        {
            Tweeds = await _tweedViewModelFactory.Create(threadTweeds.Value),
            CreateReplyTweed = new CreateReplyTweedViewModel
            {
                ParentTweedId = decodedTweedId
            }
        };
        return View(viewModel);
    }

    [HttpGet("Tweed/Create")]
    public IActionResult Create()
    {
        CreateViewModel viewModel = new();
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTweedViewModel viewModel,
        [FromServices] ICreateTweedUseCase createTweedUseCase)
    {
        if (!ModelState.IsValid) return PartialView("_CreateTweed", viewModel);

        var currentUserId = _userManager.GetUserId(User);
        var now = SystemClock.Instance.GetCurrentInstant().InUtc();

        await createTweedUseCase.CreateRootTweed(currentUserId!, viewModel.Text, now);

        _notificationManager.AppendSuccess("Tweed Posted");

        return RedirectToAction("Index", "Feed");
    }

    [HttpPost]
    public async Task<IActionResult> CreateReply(CreateReplyTweedViewModel viewModel,
        [FromServices] ICreateTweedUseCase createTweedUseCase)
    {
        if (!ModelState.IsValid) return PartialView("_CreateReplyTweed", viewModel);

        if (viewModel.ParentTweedId is null)
            return BadRequest();

        var parentTweed = await _tweedRepository.GetById(viewModel.ParentTweedId);
        if (parentTweed is null)
            return BadRequest();

        var currentUserId = _userManager.GetUserId(User);
        var now = SystemClock.Instance.GetCurrentInstant().InUtc();

        var tweed = await createTweedUseCase.CreateReplyTweed(currentUserId!, viewModel.Text, now,
            viewModel.ParentTweedId);

        _notificationManager.AppendSuccess("Reply Posted");

        return RedirectToAction("Index", "Feed");
    }

    [HttpPost]
    public async Task<IActionResult> Like(string tweedId)
    {
        var tweed = await _tweedRepository.GetById(tweedId);
        if (tweed == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        var now = SystemClock.Instance.GetCurrentInstant().InUtc();
        await _likeTweedUseCase.AddLike(tweedId, currentUserId!, now);

        var viewModel = await _tweedViewModelFactory.Create(tweed);
        return PartialView("_Tweed", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Unlike(string tweedId)
    {
        var tweed = await _tweedRepository.GetById(tweedId);
        if (tweed == null)
            return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        await _likeTweedUseCase.RemoveLike(tweedId, currentUserId!);

        var viewModel = await _tweedViewModelFactory.Create(tweed);
        return PartialView("_Tweed", viewModel);
    }
}
