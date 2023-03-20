using Microsoft.Extensions.Configuration;

namespace SongAcronymBot.Domain
{
    public interface IBotConfiguration
    {
        string SpotifyClientId { get; }
        string SpotifyClientSecret { get; }
    }

    public class BotConfiguration : IBotConfiguration
    {
        private readonly IConfiguration _configuration;

        public BotConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string SpotifyClientId => _configuration["Spotify:ClientId"];
        public string SpotifyClientSecret => _configuration["Spotify:ClientSecret"];
    }
}