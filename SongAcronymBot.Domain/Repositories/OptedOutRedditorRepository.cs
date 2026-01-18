using SongAcronymBot.Domain.Repositories.Interfaces;
using SongAcronymBot.Domain.Services.Interfaces;
using SongAcronymBot.Domain.Supabase.Models;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Repositories
{
    public class OptedOutRedditorRepository(ISupabaseService supabaseService) : BaseRepository<OptedOutRedditor>(supabaseService), IOptedOutRedditorRepository
    {
        private const int PageSize = 1000;

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

        public async Task<HashSet<string>> GetAllUsernamesAsync()
        {
            var usernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int offset = 0;
            bool hasMore = true;

            while (hasMore)
            {
                var response = await GetQueryBuilder()
                    .Select("username")
                    .Range(offset, offset + PageSize - 1)
                    .Get();

                if (response.Models.Count == 0)
                {
                    hasMore = false;
                }
                else
                {
                    foreach (var redditor in response.Models)
                    {
                        if (!string.IsNullOrEmpty(redditor.Username))
                        {
                            usernames.Add(redditor.Username);
                        }
                    }

                    // If we got less than PageSize, we've reached the end
                    hasMore = response.Models.Count == PageSize;
                    offset += PageSize;
                }
            }

            return usernames;
        }
    }
}
