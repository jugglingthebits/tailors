using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Tweed.Data.Entities;
using Tweed.Data.Indexes;

namespace Tweed.Data;

public interface IAppUserQueries
{
    Task<List<AppUser>> Search(string term);
}

public class AppUserQueries : IAppUserQueries
{
    private readonly IAsyncDocumentSession _session;

    public AppUserQueries(IAsyncDocumentSession session)
    {
        _session = session;
    }

    public async Task<List<AppUser>> Search(string term)
    {
        return await _session.Query<AppUser, AppUsers_ByUserName>()
            .Search(u => u.UserName, $"{term}*")
            .Take(20).ToListAsync();
    }
}
