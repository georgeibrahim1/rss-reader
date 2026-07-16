using System.ServiceModel.Syndication;
using System.Xml;
using Ganss.Xss;
using RssReader.Api.Models;

namespace RssReader.Api.Services;

public class FeedService
{
    private readonly HttpClient _http;
    private static readonly HtmlSanitizer _sanitizer = new();

    public FeedService(HttpClient http) => _http = http;

    public async Task<Models.Feed> AddFeedAsync(string url)
    {
        try
        {
            var xml = await _http.GetStringAsync(url);
            using var reader = XmlReader.Create(new StringReader(xml));
            var feed = SyndicationFeed.Load(reader);
            return new Models.Feed
            {
                Id = Guid.NewGuid().ToString(),
                Title = feed.Title?.Text ?? url,
                Url = url,
                AddedAt = DateTime.UtcNow
            };
        }
        catch (UriFormatException)
        { throw new InvalidOperationException("Please enter a valid URL."); }
        catch (HttpRequestException)
        { throw new InvalidOperationException("Could not reach the server. Check the URL and try again."); }
        catch (XmlException)
        { throw new InvalidOperationException("The URL doesn't appear to be a valid RSS or Atom feed."); }
        catch (Exception ex) when (ex.Message.Contains("html", StringComparison.OrdinalIgnoreCase))
        { throw new InvalidOperationException("The URL doesn't appear to be a valid RSS or Atom feed."); }
    }

    public async Task<List<Article>> FetchArticlesAsync(string url)
    {
        var xml = await _http.GetStringAsync(url);
        using var reader = XmlReader.Create(new StringReader(xml));
        var parsed = SyndicationFeed.Load(reader);
        return parsed.Items.Select(item => new Article
        {
            Id = Guid.NewGuid().ToString(),
            Title = item.Title?.Text ?? "",
            Content = _sanitizer.Sanitize(
                GetContent(item) ?? item.Summary?.Text ?? ""),
            Link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "",
            Author = item.Authors.FirstOrDefault()?.Name ?? "",
            PublishedAt = item.PublishDate.UtcDateTime
        }).ToList();
    }

    /// <summary>
    /// Extracts the richest content from a SyndicationItem.
    /// Prefers content:encoded (HTML body) over summary/description.
    /// </summary>
    private static string? GetContent(SyndicationItem item)
    {
        // Try content:encoded extension (used by WordPress, Smashing Mag, etc.)
        foreach (var ext in item.ElementExtensions)
        {
            if (ext.OuterName == "encoded" &&
                ext.OuterNamespace == "http://purl.org/rss/1.0/modules/content/")
            {
                return ext.GetObject<string>();
            }
        }
        // Fall back to the standard Content property
        if (item.Content is TextSyndicationContent textContent)
            return textContent.Text;
        return null;
    }
}
