using System.Text.Json;
using RssReader.Api.Models;

namespace RssReader.Api.Services;

public class StorageService
{
    private readonly string _filePath;
    private SubscriptionStore _store = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public StorageService(IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "data", "subscriptions.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            if (!string.IsNullOrWhiteSpace(json))
                _store = JsonSerializer.Deserialize<SubscriptionStore>(json) ?? new SubscriptionStore();
        }
    }

    public Task<List<Feed>> GetFeedsAsync()
    {
        return Task.FromResult(_store.Feeds.ToList());
    }

    public Task<Feed?> GetFeedAsync(string feedId)
    {
        return Task.FromResult(_store.Feeds.FirstOrDefault(f => f.Id.Equals(feedId)));
    }

    public Task<List<Article>> GetArticlesAsync(int page = 1, int pageSize = 20)
    {
        var articles = _store.Articles
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(articles);
    }

    public async Task AddArticlesAsync(IEnumerable<Article> articles)
    {
        foreach (var article in articles)
        {
            if (!_store.Articles.Any(a => a.Link.Equals(article.Link, StringComparison.OrdinalIgnoreCase)))
            {
                _store.Articles.Add(article);
            }
        }
        await SaveAsync();
    }

    public async Task AddFeedAsync(Feed feed)
    {
        if (_store.Feeds.Any(f => f.Url.Equals(feed.Url, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Feed already exists");
        _store.Feeds.Add(feed);
        await SaveAsync();
    }

    public async Task RemoveFeedAsync(string feedId)
    {
        _store.Feeds.RemoveAll(f => f.Id.Equals(feedId));
        _store.Articles.RemoveAll(a => a.FeedId.Equals(feedId));
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally
        {
            _lock.Release();
        }
    }
}