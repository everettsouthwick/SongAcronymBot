using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                return await GetAll().SingleAsync(x => x.Id == id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }
    }
}
