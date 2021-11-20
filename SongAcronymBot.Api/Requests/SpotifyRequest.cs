using SongAcronymBot.Repository.Models;

namespace SongAcronymBot.Api.Requests
{
    public class SpotifyRequest
    {
        public string SpotifyUrl { get; set; }
        public string? SubredditId { get; set; }
    }
}
