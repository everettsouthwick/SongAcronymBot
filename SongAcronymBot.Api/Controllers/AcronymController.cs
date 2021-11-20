using Microsoft.AspNetCore.Mvc;
using SongAcronymBot.Api.Services;
using SongAcronymBot.Repository.Models;
using SongAcronymBot.Repository.Repositories;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SongAcronymBot.Api.Controllers
{
    [Route("api/acronyms")]
    [ApiController]
    public class AcronymController : ControllerBase
    {
        private readonly IAcronymRepository _acronymRepository;

        public AcronymController(IAcronymRepository acronymRepoistory)
        {
            _acronymRepository = acronymRepoistory;
        }

        [HttpGet]
        public IQueryable<Acronym> GetAll()
        {
            return _acronymRepository.GetAll();
        }

        [HttpGet("{id}")]
        public async Task<Acronym> Get(int id)
        {
            return await _acronymRepository.GetByIdAsync(id);
        }

        [HttpPost]
        public async Task<Acronym> Add([FromBody] Acronym acronym)
        {
            return await _acronymRepository.AddAsync(acronym);
        }

        [HttpPut("{id}")]
        public async Task<Acronym> Update([FromBody] Acronym acronym)
        {
            return await _acronymRepository.UpdateAsync(acronym);
        }

        [HttpDelete("{id}")]
        public async Task<Acronym> Delete(int id)
        {
            var acronym = await _acronymRepository.GetByIdAsync(id);
            
            if (acronym == null)
                throw new ArgumentNullException($"{nameof(acronym)} must not be null");

            acronym.Enabled = false;
            return await _acronymRepository.UpdateAsync(acronym);
        }
    }
}
