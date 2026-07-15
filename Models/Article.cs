using System.Text.Json.Serialization;
namespace RssReader.Api.Models;
public class Article
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FeedId { get; set; } = "";
    [JsonIgnore] public Feed Feed { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Link { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
