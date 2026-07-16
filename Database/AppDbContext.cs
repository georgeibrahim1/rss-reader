using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RssReader.Api.Models;

namespace RssReader.Api.Database;

public class AppDbContext : IdentityDbContext<User>
{
    public DbSet<Feed> Feeds => Set<Feed>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<FeedPlaylist> FeedPlaylists => Set<FeedPlaylist>();

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FeedPlaylist>()
            .HasKey(fp => new { fp.FeedId, fp.PlaylistId });

        builder.Entity<FeedPlaylist>()
            .HasOne(fp => fp.Feed)
            .WithMany(f => f.FeedPlaylists)
            .HasForeignKey(fp => fp.FeedId);

        builder.Entity<FeedPlaylist>()
            .HasOne(fp => fp.Playlist)
            .WithMany(p => p.FeedPlaylists)
            .HasForeignKey(fp => fp.PlaylistId);

        builder.Entity<Article>()
            .HasIndex(a => a.FeedId);

        builder.Entity<Article>()
            .HasIndex(a => a.PublishedAt);

        builder.Entity<Feed>()
            .HasIndex(f => f.UserId);

        builder.Entity<Playlist>()
            .HasIndex(p => p.UserId);
    }
}
