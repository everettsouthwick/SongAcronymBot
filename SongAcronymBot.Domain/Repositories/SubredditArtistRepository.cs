using SongAcronymBot.Domain.Services;
using SongAcronymBot.Domain.Supabase.Models;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Repositories
{
    public class SubredditArtistRepository(ISupabaseService supabaseService) : BaseRepository<SubredditArtist>(supabaseService), ISubredditArtistRepository
    {
        public async Task<List<SubredditArtist>> GetBySubredditIdAsync(Guid subredditId)
        {
            var response = await GetQueryBuilder()
                .Filter("subreddit_id", Operator.Equals, subredditId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<SubredditArtist>> GetByArtistIdAsync(Guid artistId)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<SubredditArtist?> GetBySubredditAndArtistAsync(Guid subredditId, Guid artistId)
        {
            var response = await GetQueryBuilder()
                .Filter("subreddit_id", Operator.Equals, subredditId.ToString())
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Single();

            return response;
        }
    }
}
