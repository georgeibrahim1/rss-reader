using System.Text.Json.Serialization;
namespace RssReader.Api.Models;
public class Playlist
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string UserId { get; set; } = "";
    [JsonIgnore] public User User { get; set; } = null!;
    public string Emoji { get; set; } = "📁";
    [JsonIgnore] public List<FeedPlaylist> FeedPlaylists { get; set; } = [];
}
