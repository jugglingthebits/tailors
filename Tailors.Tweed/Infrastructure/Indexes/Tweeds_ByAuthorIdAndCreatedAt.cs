using Raven.Client.Documents.Indexes;
using Tailors.Tweed.Domain;

namespace Tailors.Tweed.Infrastructure.Indexes;

public class Tweeds_ByAuthorIdAndCreatedAt : AbstractIndexCreationTask<TailorsTweed>
{
    public Tweeds_ByAuthorIdAndCreatedAt()
    {
        Map = tweeds => from tweed in tweeds
            select new
            {
                tweed.AuthorId,
                tweed.CreatedAt
            };
    }
}