using OneOf;

namespace Tailors.Tweed.Domain;

public interface ICreateTweedUseCase
{
    Task<OneOf<TailorsTweed>> CreateRootTweed(string authorId, string text, DateTime createdAt);

    Task<OneOf<TailorsTweed, DomainError>> CreateReplyTweed(string authorId, string text,
        DateTime createdAt, string parentTweedId);
}

public class CreateTweedUseCase : ICreateTweedUseCase
{
    private readonly ITweedRepository _tweedRepository;

    public CreateTweedUseCase(ITweedRepository tweedRepository)
    {
        _tweedRepository = tweedRepository;
    }

    public async Task<OneOf<TailorsTweed>> CreateRootTweed(string authorId, string text,
        DateTime createdAt)
    {
        TailorsTweed tweed = new(authorId: authorId, text: text, createdAt: createdAt);
        await _tweedRepository.Create(tweed);
        return tweed;
    }

    public async Task<OneOf<TailorsTweed, DomainError>> CreateReplyTweed(string authorId,
        string text,
        DateTime createdAt, string parentTweedId)
    {
        var parentTweed = await _tweedRepository.GetById(parentTweedId);
        if (parentTweed is null)
            return new DomainError($"Parent Tweed {parentTweedId} not found");
        var threadId = parentTweed.ThreadId;
        TailorsTweed tweed = new(authorId: authorId, text: text, createdAt: createdAt, parentTweedId: parentTweedId, threadId: threadId);
        await _tweedRepository.Create(tweed);
        return tweed;
    }
}
