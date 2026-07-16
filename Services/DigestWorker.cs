using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RssReader.Api.Database;
using RssReader.Api.Models;

namespace RssReader.Api.Services;

public class DigestWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _config;
    private readonly ILogger<DigestWorker> _logger;
    private static readonly HttpClient _http = new();

    public DigestWorker(IServiceProvider sp, IConfiguration config, ILogger<DigestWorker> logger)
    {
        _sp = sp;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessDigests(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "Digest error"); }
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    public async Task SendTestEmail(string userId, CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SendDigestForUser(db, userId, ct);
    }

    private async Task ProcessDigests(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var users = await db.Users.Where(u => u.DigestFrequencyHours > 0).ToListAsync(ct);
        foreach (var user in users)
        {
            var threshold = DateTime.UtcNow.AddHours(-user.DigestFrequencyHours);
            if (user.LastDigestSent.HasValue && user.LastDigestSent > threshold) continue;
            try { await SendDigestForUser(db, user.Id, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Digest failed for {Email}", user.Email); }
        }
    }

    private async Task SendDigestForUser(AppDbContext db, string userId, CancellationToken ct)
    {
        var apiKey = EnvVal("SendGrid:ApiKey", "SENDGRID_API_KEY");
        var fromEmail = EnvVal("SendGrid:FromEmail", "SENDGRID_FROMEMAIL", "digest@rssreader.app");
        var fromName = _config["SendGrid:FromName"] ?? "RSS Reader";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("SENDGRID_API_KEY not set — skipping digest");
            return;
        }

        var user = await db.Users.FindAsync([userId], ct);
        if (user == null) return;

        var feeds = await db.Feeds
            .Where(f => f.UserId == userId && f.EmailNotifications)
            .ToListAsync(ct);

        if (feeds.Count == 0) return;

        // One latest article per feed
        var articles = new List<(Feed Feed, Article? Article)>();
        foreach (var f in feeds)
        {
            var latest = await db.Articles
                .Where(a => a.FeedId == f.Id)
                .Where(a => !user.LastDigestSent.HasValue || a.PublishedAt > user.LastDigestSent)
                .OrderByDescending(a => a.PublishedAt)
                .FirstOrDefaultAsync(ct);
            articles.Add((f, latest));
        }

        var withContent = articles.Where(a => a.Article != null).ToList();
        if (withContent.Count == 0) return;

        var html = BuildHtml(withContent, user.Email!);
        await SendGridSend(apiKey, fromEmail, fromName, user.Email!, "Your RSS Digest", html, ct);

        user.LastDigestSent = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Sent digest to {Email} — {Count} articles", user.Email, withContent.Count);
    }

    private static string BuildHtml(List<(Feed Feed, Article Article)> items, string toEmail)
    {
        var rows = string.Join("", items.Select(i =>
        {
            var feed = i.Feed;
            var a = i.Article;
            var color = feed.Color ?? "#0F7A6C";
            var preview = StripHtml(a.Content ?? "");
            if (preview.Length > 120) preview = preview[..120] + "...";
            return $"""
            <div style="border-bottom:1px solid #E4DCC9;padding:14px 0">
              <span style="background:{color};color:#fff;padding:2px 10px;border-radius:12px;font-size:11px;font-weight:700">{System.Net.WebUtility.HtmlEncode(feed.Title)}</span>
              <h3 style="margin:10px 0 4px;font-size:17px;color:#1C2B2A">{System.Net.WebUtility.HtmlEncode(a.Title)}</h3>
              <p style="color:#577590;font-size:13px;margin:0 0 10px;line-height:1.5">{System.Net.WebUtility.HtmlEncode(preview)}</p>
              <a href="{a.Link}" style="color:#0F7A6C;font-weight:600;text-decoration:none;font-size:14px" target="_blank" rel="noopener">Read more →</a>
            </div>
            """;
        }));
        return $"""
        <html><body style="font-family:-apple-system,BlinkMacSystemFont,sans-serif;background:#FBF6EC;padding:24px">
        <div style="max-width:580px;margin:0 auto;background:#fff;border-radius:16px;padding:28px 24px">
        <h2 style="color:#1C2B2A;margin:0 0 4px">RSS Reader Digest</h2>
        <p style="color:#8B8378;margin:0 0 24px;font-size:14px">Latest updates from your starred feeds.</p>
        {rows}
        <hr style="border-color:#E4DCC9;margin:24px 0 0">
        <p style="font-size:11px;color:#8B8378;margin:12px 0 6px">Sent to {toEmail} · <a href="https://deb-rss-reader-production.up.railway.app" style="color:#0F7A6C">RSS Reader</a></p>
        <p style="font-size:10px;color:#B0A99A;margin:0">You're getting this because you enabled email notifications on starred feeds. Turn them off anytime in ⚙️ Settings.</p>
        </div></body></html>
        """;
    }

    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ");
    }

    private string EnvVal(string configKey, string envKey, string fallback = "")
    {
        var val = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(val)) return val;
        val = _config[configKey];
        if (!string.IsNullOrWhiteSpace(val)) return val;
        return fallback;
    }

    private static async Task SendGridSend(string apiKey, string fromEmail, string fromName, string to, string subject, string html, CancellationToken ct)
    {
        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = to } } } },
            from = new { email = fromEmail, name = fromName },
            subject,
            content = new[] { new { type = "text/html", value = html } }
        };
        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
        {
            Headers = { { "Authorization", $"Bearer {apiKey}" } },
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"SendGrid returned {(int)res.StatusCode}: {body}");
        }
    }
}
