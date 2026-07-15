namespace RssReader.Api.Models;
public class FeedPlaylist
{
    public string FeedId { get; set; } = "";
    public string PlaylistId { get; set; } = "";
    public Feed Feed { get; set; } = null!;
    public Playlist Playlist { get; set; } = null!;
}
