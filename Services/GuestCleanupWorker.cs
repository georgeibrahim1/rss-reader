using Microsoft.EntityFrameworkCore;
using RssReader.Api.Database;

namespace RssReader.Api.Services;

public class GuestCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<GuestCleanupWorker> _logger;

    public GuestCleanupWorker(IServiceProvider sp, ILogger<GuestCleanupWorker> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once on startup, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldGuests(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guest cleanup error");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CleanupOldGuests(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-7);
        var oldGuests = await db.Users
            .Where(u => u.IsGuest && u.GuestCreatedAt != null && u.GuestCreatedAt < cutoff)
            .ToListAsync(ct);

        if (oldGuests.Count == 0) return;

        foreach (var guest in oldGuests)
        {
            // Cascade delete feeds, articles, playlists
            var feeds = await db.Feeds.Where(f => f.UserId == guest.Id).ToListAsync(ct);
            foreach (var feed in feeds)
            {
                await db.Articles.Where(a => a.FeedId == feed.Id).ExecuteDeleteAsync(ct);
            }
            await db.Feeds.Where(f => f.UserId == guest.Id).ExecuteDeleteAsync(ct);
            await db.Playlists.Where(p => p.UserId == guest.Id).ExecuteDeleteAsync(ct);
            db.Users.Remove(guest);
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Cleaned up {Count} guest accounts older than 7 days", oldGuests.Count);
    }
}
