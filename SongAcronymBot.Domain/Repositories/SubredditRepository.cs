using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Models;

namespace SongAcronymBot.Domain.Repositories
{
    public interface ISubredditRepository : IRepository<Subreddit>
    {
        Task<Subreddit> GetByIdAsync(string id);
    }

    public class SubredditRepository : Repository<Subreddit>, ISubredditRepository
    {
        public SubredditRepository(SongAcronymBotContext context) : base(context)
        {
        }

        public async Task<Subreddit> GetByIdAsync(string id)
        {
            try
            {
                var subreddit = await GetAll().SingleOrDefaultAsync(x => x.Id == id);
                return subreddit ?? throw new EntityNotFoundException($"Subreddit with id {id} not found");
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}", ex);
            }
        }
    }

    // Custom exception class
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException()
        {
        }

        public EntityNotFoundException(string message)
            : base(message)
        {
        }

        public EntityNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}