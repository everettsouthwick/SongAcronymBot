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
    public interface IRedditorRepository : IRepository<Redditor>
    {
        Task<Redditor>? GetByIdAsync(string id);
        Task<List<Redditor>>? GetAllDisabled();
    }

    public class RedditorRepository : Repository<Redditor>, IRedditorRepository
    {
        public RedditorRepository(SongAcronymBotContext context) : base(context)
        {
        }

        public async Task<Redditor>? GetByIdAsync(string id)
        {
            try
            {
                return await GetAll().SingleOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Redditor>>? GetAllDisabled()
        {
            try
            {
                return await GetAll().Where(x => !x.Enabled).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entities: {ex.Message}");
            }
        }
    }
}
