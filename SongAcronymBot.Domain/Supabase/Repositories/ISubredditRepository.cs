using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    public interface ISubredditRepository : IBaseRepository<Subreddit>
    {
        Task<Subreddit?> GetByNameAsync(string name);
        Task<List<Subreddit>> GetActiveSubredditsAsync();
    }
}
