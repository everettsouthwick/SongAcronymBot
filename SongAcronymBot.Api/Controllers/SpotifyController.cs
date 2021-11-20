using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SongAcronymBot.Api.Requests;
using SongAcronymBot.Api.Services;
using SongAcronymBot.Repository.Models;
using SongAcronymBot.Repository.Repositories;

namespace SongAcronymBot.Api.Controllers
{
    [Route("api/spotify")]
    [ApiController]
    public class SpotifyController : ControllerBase
    {
        private readonly IAcronymRepository _acronymRepository;
        private readonly ISpotifyService _spotifyService;

        public SpotifyController(IAcronymRepository acronymRepository, ISpotifyService spotifyService)
        {
            _acronymRepository = acronymRepository;
            _spotifyService = spotifyService;
        }

        [Route("artist")]
        [HttpPost]
        public async Task<List<Acronym>>? AddArtist([FromBody] SpotifyRequest request)
        {
            var acronyms = await _spotifyService.GetAcronymsFromSpotifyArtist(request);
            await _acronymRepository.AddRangeAsync(acronyms);
            return acronyms;
        }

        [Route("album")]
        [HttpPost]
        public async Task<List<Acronym>>? AddAlbum([FromBody] SpotifyRequest request)
        {
            var acronyms = await _spotifyService.GetAcronymsFromSpotifyAlbum(request);
            await _acronymRepository.AddRangeAsync(acronyms);
            return acronyms;
        }

        [Route("track")]
        [HttpPost]
        public async Task<List<Acronym>>? AddTrack([FromBody] SpotifyRequest request)
        {
            var acronym = await _spotifyService.GetAcronymsFromSpotifyTrack(request);
            await _acronymRepository.AddRangeAsync(acronym);
            return acronym;
        }
    }
}
