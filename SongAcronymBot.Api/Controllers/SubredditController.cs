using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Repositories;

namespace SongAcronymBot.Api.Controllers
{
    [Route("api/subreddit")]
    [ApiController]
    public class SubredditController(ISubredditRepository subredditRepoistory) : ControllerBase
    {
        private readonly ISubredditRepository _subredditRepository = subredditRepoistory;

        [HttpGet]
        public IQueryable<Subreddit> GetAll()
        {
            return _subredditRepository.GetAll();
        }

        [HttpGet("{id}")]
        public async Task<Subreddit> Get(string id)
        {
            return await _subredditRepository.GetByIdAsync(id);
        }

        [HttpPost]
        public async Task<Subreddit> Add([FromBody] Subreddit Subreddit)
        {
            return await _subredditRepository.AddAsync(Subreddit);
        }

        [HttpPut("{id}")]
        public async Task<Subreddit> Update([FromBody] Subreddit Subreddit)
        {
            return await _subredditRepository.UpdateAsync(Subreddit);
        }

        [HttpDelete("{id}")]
        public async Task<Subreddit> Delete(string id)
        {
            var subreddit = await _subredditRepository.GetByIdAsync(id);

            if (subreddit == null)
                throw new ArgumentNullException($"{nameof(subreddit)} must not be null");

            subreddit.Enabled = false;
            return await _subredditRepository.UpdateAsync(subreddit);
        }
    }
}
