using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RssReader.Api.Models;

namespace RssReader.Api.Database;

public static class DbSeeder
{
    private record SeedFeed(string Title, string Url, bool Starred = false);
    private record SeedPlaylist(string Name, List<SeedFeed> Feeds);

    private static readonly SeedPlaylist[] Playlists =
    [
        new("Software Architecture & Engineering", [
            new("Hacker News", "https://news.ycombinator.com/rss"),
            new("Scott Hanselman's Blog", "https://feeds.hanselman.com/ScottHanselman"),
            new("The .NET Blog", "https://devblogs.microsoft.com/dotnet/feed/"),
            new("Martin Fowler", "https://martinfowler.com/feed.atom"),
            new("The GitHub Blog", "https://github.blog/feed/"),
        ]),
        new("C++, Algorithms & Mathematics", [
            new("Sutter's Mill (Herb Sutter C++)", "https://herbsutter.com/feed/"),
            new("Quanta Magazine", "https://api.quantamagazine.org/feed/"),
            new("GeeksforGeeks", "https://www.geeksforgeeks.org/feed/"),
        ]),
        new("Hardware & Microcontrollers", [
            new("Hackaday", "https://hackaday.com/blog/feed/", true),
            new("Arduino Blog", "https://blog.arduino.cc/feed/"),
            new("Raspberry Pi News", "https://www.raspberrypi.com/news/feed/"),
        ]),
        new("Broader Technology News", [
            new("Ars Technica", "https://feeds.arstechnica.com/arstechnica/index"),
            new("The Verge", "https://www.theverge.com/rss/index.xml"),
            new("TechCrunch", "https://techcrunch.com/feed/"),
            new("Smashing Magazine", "https://www.smashingmagazine.com/feed/"),
        ]),
        new("Deep Thinking & Philosophy", [
            new("Stanford Encyclopedia of Philosophy", "https://plato.stanford.edu/rss/sep.xml"),
            new("Daily Nous", "https://dailynous.com/feed/"),
            new("Aeon Essays", "https://aeon.co/feed.rss"),
        ]),
        new("Training & Nutrition", [
            new("Stronger by Science", "https://www.strongerbyscience.com/feed/", true),
        ]),
    ];

    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        if (await db.Users.AnyAsync()) return;

        var user = new User { UserName = "test@rssreader.app", Email = "test@rssreader.app" };
        var result =         await userManager.CreateAsync(user, "Demo1234!");
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create seed user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        foreach (var pl in Playlists)
        {
            var playlist = new Playlist { Name = pl.Name, UserId = user.Id };
            db.Playlists.Add(playlist);
            await db.SaveChangesAsync();

            foreach (var sf in pl.Feeds)
            {
                var feed = new Feed
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = sf.Title,
                    Url = sf.Url,
                    UserId = user.Id,
                    Starred = sf.Starred,
                    EmailNotifications = sf.Starred,
                    AddedAt = DateTime.UtcNow
                };
                db.Feeds.Add(feed);
                await db.SaveChangesAsync();

                db.FeedPlaylists.Add(new FeedPlaylist { PlaylistId = playlist.Id, FeedId = feed.Id });
                await db.SaveChangesAsync();
            }
        }

        // Second test user — for real email testing
        var user2 = new User { UserName = "thedudegeoooooo@gmail.com", Email = "thedudegeoooooo@gmail.com" };
        result = await userManager.CreateAsync(user2, "Demo1234!");
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create seed user2: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var testFeeds = new (string Name, string Url)[]
        {
            ("Hacker News", "https://news.ycombinator.com/rss"),
            ("Ars Technica", "https://feeds.arstechnica.com/arstechnica/index"),
            ("The Verge", "https://www.theverge.com/rss/index.xml"),
            ("Hackaday", "https://hackaday.com/blog/feed/"),
        };

        foreach (var (name, url) in testFeeds)
        {
            db.Feeds.Add(new Feed
            {
                Id = Guid.NewGuid().ToString(),
                Title = name,
                Url = url,
                UserId = user2.Id,
                Starred = true,
                EmailNotifications = true,
                AddedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
    }
}
