using NodaTime;

namespace Tweed.Thread.Domain;

public class Tweed
{
    public string? Id { get; set; }
    public string? ParentTweedId { get; set; }
    public string? Text { get; init; }
    public ZonedDateTime? CreatedAt { get; init; }
    public string? AuthorId { get; set; }
    public string? ThreadId { get; set; }
}