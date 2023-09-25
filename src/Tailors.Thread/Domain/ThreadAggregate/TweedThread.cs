using System.Collections.ObjectModel;
using OneOf;
using OneOf.Types;
using Tailors.Thread.Domain.TweedAggregate;

namespace Tailors.Thread.Domain.ThreadAggregate;

public class TweedThread
{
    public TweedThread(string? id = null)
    {
        Id = id;
    }

    public string? Id { get; }
    public TweedReference Root { get; } = new();

    public class TweedReference
    {
        public string? TweedId { get; set; }

        public List<TweedReference> Replies { get; set; } = new();
    }
    
    public OneOf<Success, ReferenceNotFoundError> AddTweed(Tweed tweed)
    {
        if (tweed.ThreadId is null)
            return new ReferenceNotFoundError($"Thread {tweed.ThreadId} is missing ThreadId");

        // This is a root Tweed
        if (tweed.ParentTweedId is null)
        {
            Root.TweedId = tweed.Id;
            return new Success();
        }

        // This is a reply to a reply
        var path = FindTweedInThread(tweed.ParentTweedId);
        if (path.Count == 0)
            return new ReferenceNotFoundError($"Tweed {tweed.ParentTweedId} not found in thread {Id}");
        var parentTweedRef = path.Last();
        parentTweedRef.Replies.Add(new TweedReference
        {
            TweedId = tweed.Id
        });
        return new Success();
    }

    private ReadOnlyCollection<TweedReference> FindTweedInThread(string tweedId)
    {
        Queue<List<TweedReference>> queue = new();
        queue.Enqueue(new List<TweedReference>
        {
            Root
        });

        while (queue.Count > 0)
        {
            var currentPath = queue.Dequeue();
            var currentRef = currentPath.Last();

            if (currentRef.TweedId == tweedId)
                return currentPath.AsReadOnly();

            foreach (var reply in currentRef.Replies)
            {
                var replyPath = new List<TweedReference>(currentPath) { reply };
                queue.Enqueue(replyPath);
            }
        }

        return new ReadOnlyCollection<TweedReference>(Array.Empty<TweedReference>());
    }
}