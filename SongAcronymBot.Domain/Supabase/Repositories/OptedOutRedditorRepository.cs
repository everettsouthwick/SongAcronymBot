using SongAcronymBot.Domain.Supabase.Models;
using SongAcronymBot.Domain.Supabase.Services;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    public class OptedOutRedditorRepository(ISupabaseService supabaseService) : BaseRepository<OptedOutRedditor>(supabaseService), IOptedOutRedditorRepository
    {
        public async Task<OptedOutRedditor?> GetByUsernameAsync(string username)
        {
            var response = await GetQueryBuilder()
                .Filter("username", Operator.Equals, username)
                .Single();

            return response;
        }

        public async Task<bool> IsOptedOutAsync(string username)
        {
            var redditor = await GetByUsernameAsync(username);
            return redditor != null;
        }
    }
}
