using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Tweed.Domain;
using Tweed.Domain.Model;
using Tweed.Infrastructure.Indexes;

namespace Tweed.Infrastructure;

public class SearchService : ISearchService
{
    private readonly IAsyncDocumentSession _session;

    public SearchService(IAsyncDocumentSession session)
    {
        _session = session;
    }

    public async Task<List<AppUser>> SearchAppUsers(string term)
    {
        return await _session.Query<AppUser, AppUsers_ByUserName>()
            .Search(u => u.UserName, $"{term}*")
            .Take(20).ToListAsync();
    }
    
    public async Task<List<Domain.Model.Tweed>> SearchTweeds(string term)
    {
        return await _session.Query<Domain.Model.Tweed, Tweeds_ByText>()
            .Search(t => t.Text, term)
            .Take(20).ToListAsync();
    }
}