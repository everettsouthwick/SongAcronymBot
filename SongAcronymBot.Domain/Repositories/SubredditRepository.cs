using SongAcronymBot.Domain.Services;
using SongAcronymBot.Domain.Supabase.Models;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Repositories
{
    public class SubredditRepository(ISupabaseService supabaseService) : BaseRepository<Subreddit>(supabaseService), ISubredditRepository
    {
        public async Task<Subreddit?> GetByNameAsync(string name)
        {
            var response = await GetQueryBuilder()
                .Filter("name", Operator.Equals, name)
                .Single();

            return response;
        }

        public async Task<List<Subreddit>> GetActiveSubredditsAsync()
        {
            var response = await GetQueryBuilder()
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            return response.Models;
        }
    }
}
