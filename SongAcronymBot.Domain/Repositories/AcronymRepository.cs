using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Models;

namespace SongAcronymBot.Domain.Repositories
{
    public interface IAcronymRepository : IRepository<Acronym>
    {
        Task<Acronym> GetByIdAsync(int id);
        Task<Acronym> GetByNameAsync(string name, string subredditId);
        Task<List<Acronym>> GetAllByNameAsync(string name);
        Task<List<Acronym>> GetAllBySubredditIdAsync(string id);
        Task<List<Acronym>> GetAllBySubredditNameAsync(string name);
        Task<List<Acronym>> GetAllGlobalAcronyms();
    }

    public class AcronymRepository : Repository<Acronym>, IAcronymRepository
    {
        private readonly SongAcronymBotContext _context;

        public AcronymRepository(SongAcronymBotContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Acronym> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Set<Acronym>().SingleAsync(x => x.Id == id && x.Enabled);
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<Acronym> GetByNameAsync(string name, string subredditId)
        {
            try
            {
                return await _context.Set<Acronym>().SingleAsync(x => x.AcronymName == name && x.Subreddit != null && x.Subreddit.Id == subredditId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Acronym>> GetAllByNameAsync(string name)
        {
            try
            {
                return await _context.Set<Acronym>().Where(x => x.AcronymName == name).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Acronym>> GetAllBySubredditIdAsync(string id)
        {
            try
            {
                return await _context.Set<Acronym>().Where(x => x.Subreddit != null && x.Subreddit.Id == id).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Acronym>> GetAllBySubredditNameAsync(string name)
        {
            try
            {
                return await _context.Set<Acronym>().Where(x => x.Subreddit != null && x.Subreddit.Name == name).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Acronym>> GetAllGlobalAcronyms()
        {
            try
            {
                return await _context.Set<Acronym>().Where(x => x.Subreddit != null && x.Subreddit.Id == "global").ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }
    }
}
