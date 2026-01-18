using SongAcronymBot.Domain.Supabase.Models;
using SongAcronymBot.Domain.Supabase.Services;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    public class AlbumRepository(ISupabaseService supabaseService) : BaseRepository<Album>(supabaseService), IAlbumRepository
    {
        public async Task<Album?> GetBySpotifyIdAsync(string spotifyAlbumId)
        {
            var response = await GetQueryBuilder()
                .Filter("spotify_album_id", Operator.Equals, spotifyAlbumId)
                .Single();

            return response;
        }

        public async Task<List<Album>> GetByArtistIdAsync(Guid artistId)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<Album?> GetByArtistAndSlugAsync(Guid artistId, string slug)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Filter("slug", Operator.Equals, slug)
                .Single();

            return response;
        }
    }
}
