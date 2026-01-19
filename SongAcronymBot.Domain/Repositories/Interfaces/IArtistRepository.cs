using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Repositories.Interfaces
{
    public interface IArtistRepository : IBaseRepository<Artist>
    {
        Task<Artist?> GetBySpotifyIdAsync(string spotifyArtistId);
        Task<Artist?> GetBySlugAsync(string slug);
        Task<List<Artist>> GetActiveArtistsAsync();
    }
}
