﻿using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tweed.Domain;
using Tweed.Domain.Model;
using Tweed.Web.Helper;
using Tweed.Web.Views.Home;
using Tweed.Web.Views.Shared;

namespace Tweed.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private const int PageSize = 20;
    private readonly IFeedService _feedService;
    private readonly UserManager<User> _userManager;
    private readonly IViewModelFactory _viewModelFactory;

    public HomeController(IFeedService feedService, UserManager<User> userManager,
        IViewModelFactory viewModelFactory)
    {
        _feedService = feedService;
        _userManager = userManager;
        _viewModelFactory = viewModelFactory;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var feedViewModel = await BuildFeedViewModel(0, currentUserId);
        var viewModel = new IndexViewModel
        {
            Feed = feedViewModel
        };
        return View(viewModel);
    }

    public async Task<IActionResult> Feed(int page = 0)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        var viewModel = await BuildFeedViewModel(page, currentUserId);
        return PartialView("_Feed", viewModel);
    }

    private async Task<FeedViewModel> BuildFeedViewModel(int page, string currentUserId)
    {
        var feed = await _feedService.GetFeed(currentUserId, page, PageSize);
        var viewModel = new FeedViewModel
        {
            Page = page,
            Tweeds = await _viewModelFactory.BuildTweedViewModels(feed),
            NextPageExists = feed.Count == PageSize
        };
        return viewModel;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
            { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
