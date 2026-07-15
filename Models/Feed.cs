using System.Text.Json.Serialization;
namespace RssReader.Api.Models;
public class Feed
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = "";
    [JsonIgnore] public User User { get; set; } = null!;
    public bool Starred { get; set; }
    public string? Color { get; set; }
    public bool EmailNotifications { get; set; }
    [JsonIgnore] public List<FeedPlaylist> FeedPlaylists { get; set; } = [];
    [JsonIgnore] public List<Article> Articles { get; set; } = [];
}
