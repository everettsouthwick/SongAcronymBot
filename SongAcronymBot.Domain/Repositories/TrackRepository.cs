using SongAcronymBot.Domain.Repositories.Interfaces;
using SongAcronymBot.Domain.Services.Interfaces;
using SongAcronymBot.Domain.Supabase.Models;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Repositories
{
    public class TrackRepository(ISupabaseService supabaseService) : BaseRepository<Track>(supabaseService), ITrackRepository
    {
        public async Task<Track?> GetBySpotifyIdAsync(string spotifyTrackId)
        {
            var response = await GetQueryBuilder()
                .Filter("spotify_track_id", Operator.Equals, spotifyTrackId)
                .Single();

            return response;
        }

        public async Task<List<Track>> GetByArtistIdAsync(Guid artistId)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<Track>> GetByAlbumIdAsync(Guid albumId)
        {
            var response = await GetQueryBuilder()
                .Filter("album_id", Operator.Equals, albumId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<Track>> GetSinglesByArtistIdAsync(Guid artistId)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Filter("is_single", Operator.Equals, "true")
                .Get();

            return response.Models;
        }
    }
}
