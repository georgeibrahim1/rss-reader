using System.Text.RegularExpressions;
using CodeHollow.FeedReader;
using Ganss.Xss;
using RssReader.Api.Models;

namespace RssReader.Api.Services;

public class FeedService
{
    private readonly HttpClient _http;
    private static readonly HtmlSanitizer _sanitizer = new();

    private static readonly Dictionary<string, char> _htmlEntities = new()
    {
        ["nbsp"]   = '\u00A0', ["zwnj"]   = '\u200C', ["zwj"]    = '\u200D',
        ["lrm"]    = '\u200E', ["rlm"]    = '\u200F', ["laquo"]  = '\u00AB',
        ["raquo"]  = '\u00BB', ["mdash"]  = '\u2014', ["ndash"]  = '\u2013',
        ["lsquo"]  = '\u2018', ["rsquo"]  = '\u2019', ["ldquo"]  = '\u201C',
        ["rdquo"]  = '\u201D', ["hellip"] = '\u2026', ["sbquo"]  = '\u201A',
        ["bdquo"]  = '\u201E', ["permil"] = '\u2030', ["lsaquo"] = '\u2039',
        ["rsaquo"] = '\u203A', ["euro"]   = '\u20AC', ["copy"]   = '\u00A9',
        ["reg"]    = '\u00AE', ["trade"]  = '\u2122', ["deg"]    = '\u00B0',
        ["plusmn"] = '\u00B1', ["sup2"]   = '\u00B2', ["sup3"]   = '\u00B3',
        ["micro"]  = '\u00B5', ["middot"] = '\u00B7', ["times"]  = '\u00D7',
        ["divide"] = '\u00F7',
    };
    private static readonly Regex _entityRegex = new(@"&(\w+);", RegexOptions.Compiled);

    private static string FixXmlEntities(string xml) => _entityRegex.Replace(xml, m =>
        _htmlEntities.TryGetValue(m.Groups[1].Value, out var ch) ? ch.ToString() : m.Value);

    public FeedService(HttpClient http) => _http = http;

    public async Task<Models.Feed> AddFeedAsync(string url)
    {
        try
        {
            var xml = await _http.GetStringAsync(url);
            var feed = FeedReader.ReadFromString(FixXmlEntities(xml));
            return new Models.Feed
            {
                Id = Guid.NewGuid().ToString(),
                Title = feed.Title,
                Url = url,
                AddedAt = DateTime.UtcNow
            };
        }
        catch (UriFormatException)
        { throw new InvalidOperationException("Please enter a valid URL."); }
        catch (HttpRequestException)
        { throw new InvalidOperationException("Could not reach the server. Check the URL and try again."); }
        catch (System.Xml.XmlException)
        { throw new InvalidOperationException("The URL doesn't appear to be a valid RSS or Atom feed."); }
        catch (Exception ex) when (ex.Message.Contains("html", StringComparison.OrdinalIgnoreCase))
        { throw new InvalidOperationException("The URL doesn't appear to be a valid RSS or Atom feed."); }
    }

    public async Task<List<Article>> FetchArticlesAsync(string url)
    {
        var xml = await _http.GetStringAsync(url);
        var parsed = FeedReader.ReadFromString(FixXmlEntities(xml));
        return parsed.Items.Select(item => new Article
        {
            Id = Guid.NewGuid().ToString(),
            Title = item.Title ?? "",
            Content = _sanitizer.Sanitize(item.Content ?? item.Description ?? ""),
            Link = item.Link ?? "",
            Author = item.Author ?? "",
            PublishedAt = item.PublishingDate ?? DateTime.UtcNow
        }).ToList();
    }
}
