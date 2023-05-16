using Raven.Client.Documents.Session;
using Tweed.Domain.Model;
using Tweed.Like.Domain;
using Tweed.Tweed.Domain;

namespace Tweed.Like.Infrastructure;

public class TweedLikesRepository : ITweedLikesRepository
{
    private readonly IAsyncDocumentSession _session;

    public TweedLikesRepository(IAsyncDocumentSession session)
    {
        _session = session;
    }

    public async Task<UserLikes?> GetById(string userLikesId)
    {
        return await _session.LoadAsync<UserLikes>(userLikesId);
    }

    public async Task Create(UserLikes userLikes)
    {
        await _session.StoreAsync(userLikes);
    }

    public async Task<long> GetLikesCounter(string tweedId)
    {
        var likesCounter =
            await _session.CountersFor(tweedId).GetAsync(TheTweed.LikesCounterName);
        return likesCounter ?? 0L;
    }

    public void IncreaseLikesCounter(string tweedId)
    {
        _session.CountersFor(tweedId).Increment(TheTweed.LikesCounterName);
    }

    public void DecreaseLikesCounter(string tweedId)
    {
        _session.CountersFor(tweedId).Increment(TheTweed.LikesCounterName, -1);
    }
}
