using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Models;

namespace SongAcronymBot.Domain.Repositories
{
    public interface IAcronymRepository : IRepository<Acronym>
    {
        Task<Acronym> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Acronym> GetByNameAsync(string name, string subredditId, CancellationToken cancellationToken = default);

        Task<List<Acronym>> GetAllByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<List<Acronym>> GetAllBySubredditIdAsync(string id, CancellationToken cancellationToken = default);

        Task<List<Acronym>> GetAllBySubredditNameAsync(string name, CancellationToken cancellationToken = default);

        Task<List<Acronym>> GetAllGlobalAcronyms(CancellationToken cancellationToken = default);
    }

    public class AcronymRepository(SongAcronymBotContext context) : Repository<Acronym>(context), IAcronymRepository
    {
        private readonly AsyncLock _asyncLock = new();

        public async Task<Acronym> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var lockToken = await _asyncLock.LockAsync(cancellationToken);
                return await GetAll()
                    .SingleAsync(x => x.Id == id && x.Enabled, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<Acronym> GetByNameAsync(string name, string subredditId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var lockToken = await _asyncLock.LockAsync(cancellationToken);
                return await GetAll()
                    .SingleAsync(x => x.AcronymName == name && x.Subreddit != null && x.Subreddit.Id == subredditId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Acronym>> GetAllByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                using var lockToken = await _asyncLock.LockAsync(cancellationToken);
                return await GetAll()
                    .Where(x => x.AcronymName == name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Acronym>> GetAllBySubredditIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var lockToken = await _asyncLock.LockAsync(cancellationToken);
                return await GetAll()
                    .Where(x => x.Subreddit != null && x.Subreddit.Id == id)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Acronym>> GetAllBySubredditNameAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                using var lockToken = await _asyncLock.LockAsync(cancellationToken);
                return await GetAll()
                    .Where(x => x.Subreddit != null && x.Subreddit.Name == name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Acronym>> GetAllGlobalAcronyms(CancellationToken cancellationToken = default)
        {
            try
            {
                using var lockToken = await _asyncLock.LockAsync(cancellationToken);
                return await GetAll()
                    .Where(x => x.Subreddit != null && x.Subreddit.Id == "global")
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }
    }
}