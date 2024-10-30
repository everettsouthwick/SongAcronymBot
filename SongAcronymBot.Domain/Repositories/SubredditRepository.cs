using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Models;

namespace SongAcronymBot.Domain.Repositories
{
    public interface ISubredditRepository : IRepository<Subreddit>
    {
        Task<Subreddit> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    }

    public class SubredditRepository(SongAcronymBotContext context) : Repository<Subreddit>(context), ISubredditRepository
    {
        private readonly AsyncLock _asyncLock = new();

        public async Task<Subreddit> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    var subreddit = await GetAll()
                        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                    return subreddit ?? throw new EntityNotFoundException($"Subreddit with id {id} not found");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
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