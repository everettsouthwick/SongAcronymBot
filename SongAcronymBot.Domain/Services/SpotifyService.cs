using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SongAcronymBot.Domain.Enum;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Models.Requests;
using SongAcronymBot.Domain.Repositories;
using SpotifyAPI.Web;

namespace SongAcronymBot.Domain.Services
{
    public interface ISpotifyService
    {
        Task<List<Acronym>> GetAcronymsFromSpotifyArtistAsync(SpotifyRequest request);

        Task<List<Acronym>> GetAcronymsFromSpotifyAlbumAsync(SpotifyRequest request, bool isId = false);

        Task<List<Acronym>> GetAcronymsFromSpotifyTrackAsync(SpotifyRequest request);

        Task<Acronym> SearchAcronymAsync(string acronym);
    }

    public class SpotifyService : ISpotifyService
    {
        private readonly SpotifyConfiguration _configuration;
        private readonly IAcronymRepository _acronymRepository;
        private readonly ISubredditRepository _subredditRepository;
        private readonly IExcludedRepository _excludedRepository;

        public SpotifyService(
            IAcronymRepository acronymRepository,
            ISubredditRepository subredditRepository,
            IExcludedRepository excludedRepository,
            IOptions<SpotifyConfiguration> options)
        {
            _acronymRepository = acronymRepository;
            _subredditRepository = subredditRepository;
            _excludedRepository = excludedRepository;
            _configuration = options.Value;
        }

        public async Task<List<Acronym>> GetAcronymsFromSpotifyArtistAsync(SpotifyRequest request)
        {
            return await ExecuteWithRetry(async client =>
            {
                var acronyms = new List<Acronym>();
                var req = new ArtistsAlbumsRequest { IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album, Limit = 50, Market = "US" };
                var albums = await client.Artists.GetAlbums(GetIdFromUrl(request.SpotifyUrl), req);
                await LoadAllItems(client, albums, req, async (c, r) => await c.Artists.GetAlbums(GetIdFromUrl(request.SpotifyUrl), r));

                req.IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Single;
                var singles = await client.Artists.GetAlbums(GetIdFromUrl(request.SpotifyUrl), req);
                await LoadAllItems(client, singles, req, async (c, r) => await c.Artists.GetAlbums(GetIdFromUrl(request.SpotifyUrl), r));

                albums.Items.AddRange(singles.Items);
                albums.Items = CleanAlbums(albums.Items);

                foreach (var album in albums.Items)
                {
                    var mappedAcronyms = await GetAcronymsFromSpotifyAlbumAsync(new SpotifyRequest { SpotifyUrl = album.Id, SubredditId = request.SubredditId }, true);
                    acronyms.AddRange(mappedAcronyms);
                }

                return acronyms;
            });
        }

        public async Task<List<Acronym>> GetAcronymsFromSpotifyAlbumAsync(SpotifyRequest request, bool isId = false)
        {
            return await ExecuteWithRetry(async client =>
            {
                var acronyms = new List<Acronym>();
                var album = await client.Albums.Get(isId ? request.SpotifyUrl : GetIdFromUrl(request.SpotifyUrl), new AlbumRequest { Market = "US" });

                foreach (var track in album.Tracks.Items)
                {
                    var mappedTracks = await MapTrackToAcronymsAsync(track, album, request.SubredditId);
                    acronyms.AddRange(mappedTracks);
                }

                return acronyms;
            });
        }

        public async Task<List<Acronym>> GetAcronymsFromSpotifyTrackAsync(SpotifyRequest request)
        {
            return await ExecuteWithRetry(async client =>
            {
                var track = await client.Tracks.Get(GetIdFromUrl(request.SpotifyUrl), new TrackRequest { Market = "US" });
                return await MapTrackToAcronymsAsync(track, request.SubredditId);
            });
        }

        public async Task<Acronym> SearchAcronymAsync(string acronym)
        {
            return await ExecuteWithRetry(async client =>
            {
                var req = new SearchRequest(SearchRequest.Types.Track, acronym.ToLower()) { Market = "US", Limit = 50 };
                var response = await client.Search.Item(req);
                response.Tracks.Items = response.Tracks.Items.OrderByDescending(x => x.Popularity).ToList();

                foreach (var track in response.Tracks.Items)
                {
                    var mappedTrack = MapTrackSearchResultToAcronym(track, acronym);
                    if (mappedTrack != null) return mappedTrack;
                }

                req.Type = SearchRequest.Types.Album;
                response = await client.Search.Item(req);

                foreach (var album in response.Albums.Items)
                {
                    var mappedAlbum = MapAlbumSearchResultToAcronym(album, acronym);
                    if (mappedAlbum != null) return mappedAlbum;
                }

                req.Type = SearchRequest.Types.Artist;
                response = await client.Search.Item(req);
                response.Artists.Items = response.Artists.Items.OrderByDescending(x => x.Popularity).ToList();

                foreach (var artist in response.Artists.Items)
                {
                    var mappedArtist = MapArtistSearchResultToAcronym(artist, acronym);
                    if (mappedArtist != null) return mappedArtist;
                }

                return null;
            });
        }

        private async Task<SpotifyClient> BuildClientAsync()
        {
            var config = SpotifyClientConfig.CreateDefault();
            var request = new ClientCredentialsRequest(_configuration.ClientId, _configuration.ClientSecret);
            var response = await new OAuthClient(config).RequestToken(request);
            return new SpotifyClient(config.WithToken(response.AccessToken));
        }

        private static string GetIdFromUrl(string url)
        {
            return url.Split('/').Last().Split('?').First();
        }

        private static List<SimpleAlbum> CleanAlbums(List<SimpleAlbum> albums)
        {
            var excludeAlbums = new List<SimpleAlbum>();
            string[] whitelist = { "taylor's version", "deluxe" };
            string[] blacklist = { "karaoke", "live", "playlist", "anniversary", "remix", "disney+" };
            string[] graylist = { "-", "(", ":" };

            foreach (var album in albums)
            {
                var name = album.Name.ToLower();
                if (blacklist.Any(name.Contains) || graylist.Any(name.Contains))
                    excludeAlbums.Add(album);
                else if (!whitelist.Any(name.Contains))
                    excludeAlbums.Add(album);
            }

            return albums.Except(excludeAlbums).OrderBy(x => x.ReleaseDate).GroupBy(x => x.Name).Select(x => x.First()).ToList();
        }

        private async Task<List<Acronym>> MapTrackToAcronymsAsync(SimpleTrack track, FullAlbum album, string subredditId)
        {
            var acronyms = new List<Acronym>();

            var acronymNames = GetAcronyms(track.Name);
            foreach (var acronymName in acronymNames)
            {
                if (_excludedRepository.Contains(acronymName)) continue;
                if (album.AlbumType != "single" && track.Name == album.Name) continue;
                if (subredditId == null && acronymName.Length < 5) continue;
                if (acronymName.Length < 3) continue;

                var subreddit = await GetSubredditAsync(subredditId, acronymName);
                if (subreddit != null && await VerifyUniqueAcronymAsync(acronymName, subreddit.Id))
                    acronyms.Add(new Acronym
                    {
                        AcronymName = acronymName,
                        AcronymType = album.AlbumType == "single" ? AcronymType.Single : AcronymType.Track,
                        AlbumName = album.Name,
                        ArtistName = string.Join(", ", album.Artists.Select(x => x.Name)),
                        Enabled = true,
                        Subreddit = subreddit,
                        TrackName = track.Name,
                        YearReleased = album.ReleaseDatePrecision == "year" ? album.ReleaseDate : Convert.ToDateTime(album.ReleaseDate).Year.ToString()
                    });
            }

            return acronyms;
        }

        private async Task<List<Acronym>> MapTrackToAcronymsAsync(FullTrack track, string subredditId)
        {
            var acronyms = new List<Acronym>();
            var acronymNames = GetAcronyms(track.Name);

            foreach (var acronymName in acronymNames)
            {
                if (_excludedRepository.Contains(acronymName)) continue;
                if (subredditId == null && acronymName.Length < 5) continue;
                if (acronymName.Length < 3) continue;

                var subreddit = await GetSubredditAsync(subredditId, acronymName);
                if (subreddit != null && await VerifyUniqueAcronymAsync(acronymName, subreddit.Id))
                    acronyms.Add(new Acronym
                    {
                        AcronymName = acronymName,
                        AcronymType = track.Album.AlbumType == "single" ? AcronymType.Single : AcronymType.Track,
                        AlbumName = track.Album.Name,
                        ArtistName = string.Join(", ", track.Album.Artists.Select(x => x.Name)),
                        Enabled = true,
                        Subreddit = subreddit,
                        TrackName = track.Name,
                        YearReleased = track.Album.ReleaseDatePrecision == "year" ? track.Album.ReleaseDate : Convert.ToDateTime(track.Album.ReleaseDate).Year.ToString()
                    });
            }

            return acronyms;
        }

        private Acronym MapArtistSearchResultToAcronym(FullArtist artist, string acronym)
        {
            var acronymNames = GetAcronyms(artist.Name);
            if (!acronymNames.Any(x => x.Equals(acronym, StringComparison.OrdinalIgnoreCase)))
                return null;

            return new Acronym
            {
                AcronymName = acronym.ToUpper(),
                AcronymType = AcronymType.Artist,
                ArtistName = artist.Name,
                Enabled = true
            };
        }

        private Acronym MapAlbumSearchResultToAcronym(SimpleAlbum album, string acronym)
        {
            var acronymNames = GetAcronyms(album.Name);
            if (!acronymNames.Any(x => x.Equals(acronym, StringComparison.OrdinalIgnoreCase)))
                return null;

            return new Acronym
            {
                AcronymName = acronym.ToUpper(),
                AcronymType = AcronymType.Album,
                AlbumName = album.Name,
                ArtistName = string.Join(", ", album.Artists.Select(x => x.Name)),
                Enabled = true,
                YearReleased = album.ReleaseDatePrecision == "year" ? album.ReleaseDate : Convert.ToDateTime(album.ReleaseDate).Year.ToString()
            };
        }

        private Acronym MapTrackSearchResultToAcronym(FullTrack track, string acronym)
        {
            var acronymNames = GetAcronyms(track.Name);
            if (!acronymNames.Any(x => x.Equals(acronym, StringComparison.OrdinalIgnoreCase)))
                return null;

            return new Acronym
            {
                AcronymName = acronym.ToUpper(),
                AcronymType = track.Album.AlbumType == "single" ? AcronymType.Single : AcronymType.Track,
                AlbumName = track.Album.Name,
                ArtistName = string.Join(", ", track.Album.Artists.Select(x => x.Name)),
                Enabled = true,
                TrackName = track.Name,
                YearReleased = track.Album.ReleaseDatePrecision == "year" ? track.Album.ReleaseDate : Convert.ToDateTime(track.Album.ReleaseDate).Year.ToString()
            };
        }

        private static List<string> GetAcronyms(string name)
        {
            name = name.Split('(').First().Split('-').First();
            name = Regex.Replace(name, @"[^a-zA-Z0-9]", "").ToUpperInvariant().Trim();
            var acronym = string.Concat(name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s[0]));

            var acronyms = new List<string> { acronym };
            if (acronym.Contains('&'))
            {
                acronyms.Add(acronym.Replace('&', 'A'));
                acronyms.Add(acronym.Replace("&", ""));
            }
            if (acronym.Contains('/'))
                acronyms.Add(acronym.Replace("/", ""));

            return acronyms;
        }

        private async Task<Subreddit> GetSubredditAsync(string subredditId, string acronym)
        {
            return acronym.Length >= 5
                ? await _subredditRepository.GetByIdAsync("global")
                : await _subredditRepository.GetByIdAsync(subredditId);
        }

        private async Task<bool> VerifyUniqueAcronymAsync(string acronym, string subredditId)
        {
            var existingAcronyms = await _acronymRepository.GetAllBySubredditIdAsync(subredditId);
            return !existingAcronyms.Any(x => x.AcronymName == acronym);
        }

        private async Task<T> ExecuteWithRetry<T>(Func<SpotifyClient, Task<T>> action)
        {
            int maxRetries = 5;
            int delay = 1000;
            int retries = 0;

            while (true)
            {
                try
                {
                    var client = await BuildClientAsync();
                    return await action(client);
                }
                catch (APITooManyRequestsException ex)
                {
                    if (retries >= maxRetries)
                        throw;

                    if (ex.Response.Headers.TryGetValue("Retry-After", out var value) && int.TryParse(value, out var retryAfter))
                        await Task.Delay(retryAfter * 1000);
                    else
                        await Task.Delay(delay);

                    delay *= 2;
                    retries++;
                }
            }
        }

        private async Task LoadAllItems<T>(SpotifyClient client, Paging<T> paging, ArtistsAlbumsRequest req, Func<SpotifyClient, ArtistsAlbumsRequest, Task<Paging<T>>> getAlbumsFunc)
        {
            int i = 1;
            while (paging.Items.Count < paging.Total)
            {
                req.Offset = i * 50;
                var nextPage = await getAlbumsFunc(client, req);
                paging.Items.AddRange(nextPage.Items);
                i++;
            }
        }
    }
}