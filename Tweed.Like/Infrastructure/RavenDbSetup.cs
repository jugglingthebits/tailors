using Raven.Client.Documents;
using Tweed.Domain.Model;

namespace Tweed.Infrastructure;

public static class RavenDbSetup
{
    public static void PreInitializeLikes(this IDocumentStore store)
    {
        store.ApplyCustomConventions();
    }
    
    private static void ApplyCustomConventions(this IDocumentStore store)
    {
        store.Conventions.RegisterAsyncIdConvention<UserLikes>((_, userLikes) =>
        {
            ArgumentNullException.ThrowIfNull(userLikes.UserId);
            return Task.FromResult(UserLikes.BuildId(userLikes.UserId));
        });
    }
}
