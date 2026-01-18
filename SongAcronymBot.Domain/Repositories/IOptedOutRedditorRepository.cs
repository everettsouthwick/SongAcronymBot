using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Repositories
{
    public interface IOptedOutRedditorRepository : IBaseRepository<OptedOutRedditor>
    {
        Task<OptedOutRedditor?> GetByUsernameAsync(string username);
        Task<bool> IsOptedOutAsync(string username);
        /// <summary>
        /// Gets all opted-out usernames as a HashSet for O(1) lookups.
        /// This method handles pagination to retrieve all records beyond Supabase's 1000 row limit.
        /// </summary>
        Task<HashSet<string>> GetAllUsernamesAsync();
    }
}
