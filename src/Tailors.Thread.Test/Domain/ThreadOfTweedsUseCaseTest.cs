using Moq;
using Tailors.Thread.Domain.ThreadAggregate;
using Tailors.Tweed.Domain;
using Xunit;

namespace Tailors.Thread.Test.Domain;

public class ThreadOfTweedsUseCaseTest
{
    private static readonly DateTime FixedDateTime = new DateTime(2022, 11, 18, 15, 20, 0);
    private readonly ThreadOfTweedsUseCase _sut;
    private readonly Mock<ITweedRepository> _tweedRepositoryMock = new();
    private readonly Mock<ITweedThreadRepository> _tweedThreadRepositoryMock = new();

    public ThreadOfTweedsUseCaseTest()
    {
        _sut = new ThreadOfTweedsUseCase(_tweedThreadRepositoryMock.Object,
            _tweedRepositoryMock.Object);
    }

    [Fact]
    public async Task GetThreadTweedsForTweed_ShouldReturnFail_WhenTweedNotFound()
    {
        var tweeds = await _sut.GetThreadTweedsForTweed("unknownTweedId");

        Assert.True(tweeds.IsT1);
    }

    [Fact]
    public async Task GetThreadTweedsForTweed_ShouldReturnRootTweed_WhenThereIsOnlyRoot()
    {
        Tweed.Domain.Tweed rootTweed = new(id: "rootTweedId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        _tweedRepositoryMock.Setup(m => m.GetById("rootTweedId")).ReturnsAsync(rootTweed);
        TweedThread thread = new(id: "threadId");
        thread.AddTweed(rootTweed);
        _tweedThreadRepositoryMock.Setup(t => t.GetById(thread.Id!)).ReturnsAsync(thread);

        var tweeds = await _sut.GetThreadTweedsForTweed("rootTweedId");

        Assert.True(tweeds.IsT0);
        Assert.Empty(tweeds.AsT0);
    }

    [Fact]
    public async Task GetThreadTweedsForTweed_ShouldReturnTweeds_WhenThereIsOneLeadingTweed()
    {
        Tweed.Domain.Tweed rootTweed = new(id: "rootTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        Tweed.Domain.Tweed tweed = new(id: "tweedId", parentTweedId: rootTweed.Id!, threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        _tweedRepositoryMock.Setup(m => m.GetById(tweed.Id!)).ReturnsAsync(tweed);

        TweedThread thread = new(id: "threadId");
        thread.AddTweed(rootTweed);
        thread.AddTweed(tweed);
           
        _tweedThreadRepositoryMock.Setup(t => t.GetById(thread.Id!)).ReturnsAsync(thread);

        _tweedRepositoryMock.Setup(m =>
                m.GetByIds(MoqExtensions.CollectionMatcher(new[] { "rootTweedId", "tweedId" })))
            .ReturnsAsync(
                new Dictionary<string, Tweed.Domain.Tweed>
                {
                    { rootTweed.Id!, rootTweed },
                    { tweed.Id!, tweed }
                });

        var tweeds = await _sut.GetThreadTweedsForTweed("tweedId");

        Assert.True(tweeds.IsT0);
        Assert.Equal("rootTweedId", tweeds.AsT0[0].Id);
        Assert.Equal("tweedId", tweeds.AsT0[1].Id);
    }

    [Fact]
    public async Task GetThreadTweedsForTweed_ShouldReturnTweeds_WhenThereIsAnotherBranch()
    {
        Tweed.Domain.Tweed rootTweed = new(id: "rootTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        Tweed.Domain.Tweed parentTweed = new(id: "parentTweedId", parentTweedId: "rootTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        Tweed.Domain.Tweed tweed = new(id: "tweedId", parentTweedId: "parentTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        _tweedRepositoryMock.Setup(m => m.GetById(tweed.Id!)).ReturnsAsync(tweed);
        Tweed.Domain.Tweed otherTweed = new(id: "otherTweedId", parentTweedId: "rootTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);

        TweedThread thread = new(id: "threadId");
        thread.AddTweed(rootTweed);
        thread.AddTweed(parentTweed);
        thread.AddTweed(tweed);
        thread.AddTweed(otherTweed);
        _tweedThreadRepositoryMock.Setup(t => t.GetById(thread.Id!)).ReturnsAsync(thread);
        _tweedRepositoryMock.Setup(m =>
                m.GetByIds(MoqExtensions.CollectionMatcher(new[] { "rootTweedId", "parentTweedId", "tweedId" })))
            .ReturnsAsync(
                new Dictionary<string, Tweed.Domain.Tweed>
                {
                    { rootTweed.Id!, rootTweed },
                    { parentTweed.Id!, parentTweed },
                    { tweed.Id!, tweed }
                });

        var tweeds = await _sut.GetThreadTweedsForTweed("tweedId");

        Assert.True(tweeds.IsT0);
        Assert.Equal("rootTweedId", tweeds.AsT0[0].Id);
        Assert.Equal("parentTweedId", tweeds.AsT0[1].Id);
        Assert.Equal("tweedId", tweeds.AsT0[2].Id);
    }

    [Fact]
    public async Task AddTweedToThread_ShouldCreateTweed_WhenTweedIdIsNull()
    {
        Tweed.Domain.Tweed tweed = new(id: "tweedId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        _tweedRepositoryMock.Setup(m => m.GetById(tweed.Id!)).ReturnsAsync(tweed);
        _tweedThreadRepositoryMock.Setup(t => t.Create()).ReturnsAsync(new TweedThread(id: "threadId"));

        var result = await _sut.AddTweedToThread("tweedId");

        Assert.True(result.IsT0);
        Assert.Equal("threadId", tweed.ThreadId);
    }

    [Fact]
    public async Task AddTweedToThread_ShouldSetRootTweed_IfParentTweedIdIsNull()
    {
        Tweed.Domain.Tweed tweed = new(id: "tweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        _tweedRepositoryMock.Setup(m => m.GetById(tweed.Id!)).ReturnsAsync(tweed);
        TweedThread thread = new();
        _tweedThreadRepositoryMock.Setup(m => m.GetById("threadId")).ReturnsAsync(thread);

        var result = await _sut.AddTweedToThread("tweedId");

        Assert.True(result.IsT0);
        Assert.Equal("tweedId", thread.Root?.TweedId);
    }

    [Fact]
    public async Task AddTweedToThread_ShouldInsertReplyToRootTweed()
    {
        Tweed.Domain.Tweed rootTweed = new(id: "rootTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        _tweedRepositoryMock.Setup(m => m.GetById(rootTweed.Id!)).ReturnsAsync(rootTweed);
        Tweed.Domain.Tweed tweed = new(id: "tweedId", parentTweedId: "rootTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        _tweedRepositoryMock.Setup(m => m.GetById(tweed.Id!)).ReturnsAsync(tweed);
        TweedThread thread = new(id: "threadId");
        thread.AddTweed(rootTweed);

        _tweedThreadRepositoryMock.Setup(m => m.GetById("threadId")).ReturnsAsync(thread);

        var result = await _sut.AddTweedToThread("tweedId");

        Assert.True(result.IsT0);
        Assert.Equal("tweedId", thread.Root?.Replies[0].TweedId);
    }

    [Fact]
    public async Task AddTweedToThread_ShouldInsertReplyToReplyTweed()
    {
        Tweed.Domain.Tweed rootTweed = new(id: "rootTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        Tweed.Domain.Tweed replyTweed = new(id: "replyTweedId", parentTweedId: "rootTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        Tweed.Domain.Tweed tweed = new(id: "tweedId", parentTweedId: "replyTweedId", threadId: "threadId", authorId: "authorId", createdAt: FixedDateTime, text: string.Empty);
        _tweedRepositoryMock.Setup(m => m.GetById(tweed.Id!)).ReturnsAsync(tweed);
        
        TweedThread thread = new(id: "threadId");
        thread.AddTweed(rootTweed);
        thread.AddTweed(replyTweed);
        
        _tweedThreadRepositoryMock.Setup(m => m.GetById("threadId")).ReturnsAsync(thread);

        var result = await _sut.AddTweedToThread("tweedId");

        Assert.True(result.IsT0);
        Assert.Equal("tweedId", thread.Root?.Replies[0].Replies[0].TweedId);
    }
}
