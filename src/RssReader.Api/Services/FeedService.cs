using CodeHollow.FeedReader;
using Ganss.Xss;
using RssReader.Api.Models;

namespace RssReader.Api.Services;

public class FeedService
{
    private readonly StorageService _store;
    private static readonly HtmlSanitizer _sanitizer = new();

    public FeedService(StorageService store)
    {
        _store = store;
    }

    public async Task<Models.Feed> AddFeedAsync(string url)
    {
        var feed = await FeedReader.ReadAsync(url);
        var newFeed = new Models.Feed
        {
            Id = Guid.NewGuid().ToString(),
            Title = feed.Title,
            Url = url,
            AddedAt = DateTime.UtcNow
        };
        await _store.AddFeedAsync(newFeed);
        return newFeed;
    }

    public async Task RefreshFeedAsync(string feedId)
    {
        var feed = await _store.GetFeedAsync(feedId)
            ?? throw new InvalidOperationException("Feed not found");

        var parsed = await FeedReader.ReadAsync(feed.Url);

        var articles = parsed.Items.Select(item => new Article
        {
            Id = Guid.NewGuid().ToString(),
            FeedId = feedId,
            Title = item.Title ?? "",
            Description = _sanitizer.Sanitize(item.Description ?? item.Content ?? ""),
            Link = item.Link ?? "",
            Author = item.Author ?? "",
            PublishedAt = item.PublishingDate ?? DateTime.UtcNow
        });

        await _store.AddArticlesAsync(articles);
    }
}
