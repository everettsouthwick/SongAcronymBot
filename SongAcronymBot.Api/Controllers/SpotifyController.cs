using Microsoft.AspNetCore.Mvc;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Models.Requests;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

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
            var acronyms = await _spotifyService.GetAcronymsFromSpotifyArtistAsync(request);
            await _acronymRepository.AddRangeAsync(acronyms);
            return acronyms;
        }

        [Route("album")]
        [HttpPost]
        public async Task<List<Acronym>>? AddAlbum([FromBody] SpotifyRequest request)
        {
            var acronyms = await _spotifyService.GetAcronymsFromSpotifyAlbumAsync(request);
            await _acronymRepository.AddRangeAsync(acronyms);
            return acronyms;
        }

        [Route("track")]
        [HttpPost]
        public async Task<List<Acronym>>? AddTrack([FromBody] SpotifyRequest request)
        {
            var acronym = await _spotifyService.GetAcronymsFromSpotifyTrackAsync(request);
            await _acronymRepository.AddRangeAsync(acronym);
            return acronym;
        }

        [Route("search")]
        [HttpPost]
        public async Task<Acronym>? Search([FromBody] string acronym)
        {
            var result = await _spotifyService.SearchAcronymAsync(acronym);
            return result;
        }
    }
}
