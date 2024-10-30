using Microsoft.AspNetCore.Mvc;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Models.Requests;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

namespace SongAcronymBot.Api.Controllers
{
    [Route("api/spotify")]
    [ApiController]
    public class SpotifyController(IAcronymRepository acronymRepository, ISpotifyService spotifyService) : ControllerBase
    {
        private readonly IAcronymRepository _acronymRepository = acronymRepository;
        private readonly ISpotifyService _spotifyService = spotifyService;

        [Route("artist")]
        [HttpPost]
        public async Task<List<string>>? AddArtist([FromBody] SpotifyRequest request)
        {
            var acronyms = await _spotifyService.GetAcronymsFromSpotifyArtistAsync(request);
            await _acronymRepository.AddRangeAsync(acronyms);
            return acronyms.Select(x => $"{x.Id}: {x.AcronymName} => {(x.TrackName == null ? x.AlbumName : x.TrackName)}").ToList();
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
