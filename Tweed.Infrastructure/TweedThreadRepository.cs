using Raven.Client.Documents.Session;
using Tweed.Domain;
using Tweed.Domain.Model;

namespace Tweed.Infrastructure;

public class TweedThreadRepository : ITweedThreadRepository
{
    private readonly IAsyncDocumentSession _session;

    public TweedThreadRepository(IAsyncDocumentSession session)
    {
        _session = session;
    }

    public async Task Create(TweedThread thread)
    {
        await _session.StoreAsync(thread);
    }

    public async Task<TweedThread?> GetById(string threadId)
    {
        return await _session.LoadAsync<TweedThread>(threadId);
    }
}