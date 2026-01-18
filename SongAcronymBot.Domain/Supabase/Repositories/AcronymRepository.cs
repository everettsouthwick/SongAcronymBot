using SongAcronymBot.Domain.Supabase.Models;
using SongAcronymBot.Domain.Supabase.Services;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    public class AcronymRepository(ISupabaseService supabaseService) : BaseRepository<Acronym>(supabaseService), IAcronymRepository
    {
        public async Task<Acronym?> GetByAcronymTextAsync(string acronym)
        {
            var response = await GetQueryBuilder()
                .Filter("acronym", Operator.Equals, acronym)
                .Single();

            return response;
        }

        public async Task<Acronym?> GetByArtistAndAcronymTextAsync(Guid artistId, string acronym)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Filter("acronym", Operator.Equals, acronym)
                .Single();

            return response;
        }

        public async Task<List<Acronym>> GetByArtistIdAsync(Guid artistId)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<Acronym>> GetByAlbumIdAsync(Guid albumId)
        {
            var response = await GetQueryBuilder()
                .Filter("album_id", Operator.Equals, albumId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<Acronym>> GetByTrackIdAsync(Guid trackId)
        {
            var response = await GetQueryBuilder()
                .Filter("track_id", Operator.Equals, trackId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<Acronym>> GetByTypeAsync(AcronymType type)
        {
            var response = await GetQueryBuilder()
                .Filter("acronym_type", Operator.Equals, type.ToString().ToLowerInvariant())
                .Get();

            return response.Models;
        }

        public async Task<List<Acronym>> GetActiveAcronymsAsync()
        {
            var response = await GetQueryBuilder()
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            return response.Models;
        }
    }
}
