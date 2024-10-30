using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Repositories;

namespace SongAcronymBot.Api.Controllers
{
    [Route("api/redditor")]
    [ApiController]
    public class RedditorController(IRedditorRepository redditorRepoistory) : ControllerBase
    {
        private readonly IRedditorRepository _redditorRepository = redditorRepoistory;

        [HttpGet]
        public IQueryable<Redditor> GetAll()
        {
            return _redditorRepository.GetAll();
        }

        [HttpGet("{id}")]
        public async Task<Redditor> Get(string id)
        {
            return await _redditorRepository.GetByIdAsync(id);
        }

        [HttpPost]
        public async Task<Redditor> Add([FromBody] Redditor Redditor)
        {
            return await _redditorRepository.AddAsync(Redditor);
        }

        [HttpPut("{id}")]
        public async Task<Redditor> Update([FromBody] Redditor Redditor)
        {
            return await _redditorRepository.UpdateAsync(Redditor);
        }

        [HttpDelete("{id}")]
        public async Task<Redditor> Delete(string id)
        {
            var redditor = await _redditorRepository.GetByIdAsync(id);

            if (redditor == null)
                throw new ArgumentNullException($"{nameof(redditor)} must not be null");

            redditor.Enabled = false;
            return await _redditorRepository.UpdateAsync(redditor);
        }
    }
}
