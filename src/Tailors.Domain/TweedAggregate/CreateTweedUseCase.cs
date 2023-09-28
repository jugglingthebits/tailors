using OneOf;

namespace Tailors.Domain.TweedAggregate;

public interface ICreateTweedUseCase
{
    Task<OneOf<Tweed>> CreateRootTweed(string authorId, string text, DateTime createdAt);

    Task<OneOf<Tweed, TweedError>> CreateReplyTweed(string authorId, string text,
        DateTime createdAt, string parentTweedId);
}

public class CreateTweedUseCase : ICreateTweedUseCase
{
    private readonly ITweedRepository _tweedRepository;

    public CreateTweedUseCase(ITweedRepository tweedRepository)
    {
        _tweedRepository = tweedRepository;
    }

    public async Task<OneOf<Tweed>> CreateRootTweed(string authorId, string text,
        DateTime createdAt)
    {
        Tweed tweed = new(authorId: authorId, text: text, createdAt: createdAt);
        await _tweedRepository.Create(tweed);
        return tweed;
    }

    public async Task<OneOf<Tweed, TweedError>> CreateReplyTweed(string authorId,
        string text,
        DateTime createdAt, string parentTweedId)
    {
        var parentTweed = await _tweedRepository.GetById(parentTweedId);
        if (parentTweed is null)
            return new TweedError($"Parent Tweed {parentTweedId} not found");
        
        var threadId = parentTweed.ThreadId;
        Tweed tweed = new(authorId: authorId, text: text, createdAt: createdAt, parentTweedId: parentTweedId, threadId: threadId);
        await _tweedRepository.Create(tweed);
        return tweed;
    }
}
