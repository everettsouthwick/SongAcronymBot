using SongAcronymBot.Domain.Supabase.Models;
using SongAcronymBot.Domain.Supabase.Services;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    public class ArtistRepository(ISupabaseService supabaseService) : BaseRepository<Artist>(supabaseService), IArtistRepository
    {
        public async Task<Artist?> GetBySpotifyIdAsync(string spotifyArtistId)
        {
            var response = await GetQueryBuilder()
                .Filter("spotify_artist_id", Operator.Equals, spotifyArtistId)
                .Single();

            return response;
        }

        public async Task<Artist?> GetBySlugAsync(string slug)
        {
            var response = await GetQueryBuilder()
                .Filter("slug", Operator.Equals, slug)
                .Single();

            return response;
        }

        public async Task<List<Artist>> GetActiveArtistsAsync()
        {
            var response = await GetQueryBuilder()
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            return response.Models;
        }
    }
}
