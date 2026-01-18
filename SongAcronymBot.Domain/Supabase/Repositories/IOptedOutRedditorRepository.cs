using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    public interface IOptedOutRedditorRepository : IBaseRepository<OptedOutRedditor>
    {
        Task<OptedOutRedditor?> GetByUsernameAsync(string username);
        Task<bool> IsOptedOutAsync(string username);
    }
}
