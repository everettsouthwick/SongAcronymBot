using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    public interface IAlbumRepository : IBaseRepository<Album>
    {
        Task<Album?> GetBySpotifyIdAsync(string spotifyAlbumId);
        Task<List<Album>> GetByArtistIdAsync(Guid artistId);
        Task<Album?> GetByArtistAndSlugAsync(Guid artistId, string slug);
    }
}
