using FluentResults;
using NodaTime;

namespace Tweed.Thread.Domain;

public interface ICreateTweedUseCase
{
    Task<Result<Tweed>> CreateRootTweed(string authorId, string text, ZonedDateTime createdAt);

    Task<Result<Tweed>> CreateReplyTweed(string authorId, string text, ZonedDateTime createdAt,
        string parentTweedId);
}

public class CreateTweedUseCase : ICreateTweedUseCase
{
    private readonly ITweedRepository _tweedRepository;
    private readonly ITweedThreadRepository _tweedThreadRepository;

    public CreateTweedUseCase(ITweedRepository tweedRepository, ITweedThreadRepository tweedThreadRepository)
    {
        _tweedRepository = tweedRepository;
        _tweedThreadRepository = tweedThreadRepository;
    }

    public async Task<Result<Tweed>> CreateRootTweed(string authorId, string text,
        ZonedDateTime createdAt)
    {
        Tweed tweed = new()
        {
            AuthorId = authorId,
            Text = text,
            CreatedAt = createdAt
        };
        await _tweedRepository.Create(tweed);

        var thread = await CreateThread(tweed.Id!);
        tweed.ThreadId = thread.Id;
        return tweed;
    }

    public async Task<Result<Tweed>> CreateReplyTweed(string authorId, string text,
        ZonedDateTime createdAt, string parentTweedId)
    {
        var parentTweed = await _tweedRepository.GetById(parentTweedId);
        if (parentTweed is null)
            return Result.Fail(new ReferenceNotFoundError($"Parent Tweed {parentTweedId} not found"));
        var threadId = parentTweed.ThreadId;
        Tweed tweed = new()
        {
            AuthorId = authorId,
            Text = text,
            ParentTweedId = parentTweedId,
            ThreadId = threadId,
            CreatedAt = createdAt
        };
        await _tweedRepository.Create(tweed);
        return tweed;
    }

    private async Task<TweedThread> CreateThread(string tweedId)
    {
        TweedThread thread = new()
        {
            Root =
            {
                TweedId = tweedId
            }
        };
        await _tweedThreadRepository.Create(thread);
        return thread;
    }
}