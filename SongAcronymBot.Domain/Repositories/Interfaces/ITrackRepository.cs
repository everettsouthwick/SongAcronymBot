using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Repositories.Interfaces
{
    public interface ITrackRepository : IBaseRepository<Track>
    {
        Task<Track?> GetBySpotifyIdAsync(string spotifyTrackId);
        Task<List<Track>> GetByArtistIdAsync(Guid artistId);
        Task<List<Track>> GetByAlbumIdAsync(Guid albumId);
        Task<List<Track>> GetSinglesByArtistIdAsync(Guid artistId);
    }
}
