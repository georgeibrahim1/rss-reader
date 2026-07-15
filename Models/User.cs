using Microsoft.AspNetCore.Identity;
namespace RssReader.Api.Models;
public class User : IdentityUser
{
    public int DigestFrequencyHours { get; set; } = 24;
    public DateTime? LastDigestSent { get; set; }
}
