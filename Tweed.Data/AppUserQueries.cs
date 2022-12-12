using NodaTime;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Tweed.Data.Entities;

namespace Tweed.Data;

public interface IAppUserQueries
{
    Task AddFollower(string leaderId, string followerId, ZonedDateTime createdAt);
    Task RemoveFollower(string leaderId, string userId);
    Task<int> GetFollowerCount(string userId);
    Task AddLike(string userId, string tweedId, ZonedDateTime createdAt);
    Task RemoveLike(string userId, string tweedId);
    Task<List<AppUser>> Search(string abc);
}

public class AppUserQueries : IAppUserQueries
{
    private readonly IAsyncDocumentSession _session;

    public AppUserQueries(IAsyncDocumentSession session)
    {
        _session = session;
    }

    public async Task AddFollower(string leaderId, string followerId, ZonedDateTime createdAt)
    {
        var follower = await _session.LoadAsync<AppUser>(followerId);
        if (follower.Follows.Any(f => f.LeaderId == leaderId))
            return;

        follower.Follows.Add(new Follows
        {
            LeaderId = leaderId,
            CreatedAt = createdAt
        });
    }

    public async Task RemoveFollower(string leaderId, string userId)
    {
        var follower = await _session.LoadAsync<AppUser>(userId);
        follower.Follows.RemoveAll(f => f.LeaderId == leaderId);
    }

    public async Task<int> GetFollowerCount(string userId)
    {
        var result = await _session.Query<AppUsers_FollowerCount.Result, AppUsers_FollowerCount>()
            .Where(r => r.UserId == userId)
            .FirstOrDefaultAsync();

        return result?.FollowerCount ?? 0;
    }

    public async Task AddLike(string userId, string tweedId, ZonedDateTime createdAt)
    {
        var user = await _session.LoadAsync<AppUser>(userId);
        if (user.Likes.Any(l => l.TweedId == tweedId))
            return;

        user.Likes.Add(new TweedLike
        {
            TweedId = tweedId,
            CreatedAt = createdAt
        });
    }

    public async Task RemoveLike(string userId, string tweedId)
    {
        var user = await _session.LoadAsync<AppUser>(userId);

        user.Likes.RemoveAll(l => l.TweedId == tweedId);
    }

    public async Task<List<AppUser>> Search(string term)
    {
        return await _session.Query<AppUser, AppUsers_ByUserName>()
            .Search(u => u.UserName, $"{term}*")
            .Take(20).ToListAsync();
    }
}
