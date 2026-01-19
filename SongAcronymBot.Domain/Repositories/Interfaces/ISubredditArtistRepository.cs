using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Repositories.Interfaces
{
    public interface ISubredditArtistRepository : IBaseRepository<SubredditArtist>
    {
        Task<List<SubredditArtist>> GetBySubredditIdAsync(Guid subredditId);
        Task<List<SubredditArtist>> GetByArtistIdAsync(Guid artistId);
        Task<SubredditArtist?> GetBySubredditAndArtistAsync(Guid subredditId, Guid artistId);
    }
}
