using SongAcronymBot.Api.Requests;
using SongAcronymBot.Repository.Enum;
using SongAcronymBot.Repository.Models;
using SongAcronymBot.Repository.Repositories;
using SpotifyAPI.Web;
using System.Text.RegularExpressions;

namespace SongAcronymBot.Api.Services
{
    public interface ISpotifyService
    {
        Task<List<Acronym>>? GetAcronymsFromSpotifyArtist(SpotifyRequest request);
        Task<List<Acronym>>? GetAcronymsFromSpotifyAlbum(SpotifyRequest request, bool isId = false);
        Task<List<Acronym>>? GetAcronymsFromSpotifyTrack(SpotifyRequest request);
    }

    public class SpotifyService : ISpotifyService
    {
        private readonly IAcronymRepository _acronymRepository;
        private readonly ISubredditRepository _subredditRepository;
        private static List<string> ExcludedAcronyms = File.ReadAllLines($"{Directory.GetCurrentDirectory()}\\Files\\ExcludedAcronyms.txt").ToList();

        public SpotifyService(IAcronymRepository acronymRepository, ISubredditRepository subredditRepoistory)
        {
            _acronymRepository = acronymRepository;
            _subredditRepository = subredditRepoistory;
        }

        public async Task<List<Acronym>>? GetAcronymsFromSpotifyArtist(SpotifyRequest request)
        {
            var client = await BuildClientAsync();
            var acronyms = new List<Acronym>();
            var req = new ArtistsAlbumsRequest { IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album, Limit = 50, Market = "US" };

            var albums = await client.Artists.GetAlbums(GetIdFromUrl(request.SpotifyUrl), req);
            
            if (albums.Items == null)
                throw new ArgumentNullException($"{nameof(albums)} is null.");

            var i = 1;
            while (albums.Items.Count < albums.Total)
            {
                req.Offset = i * 50;
                albums.Items.AddRange((await client.Artists.GetAlbums(GetIdFromUrl(request.SpotifyUrl), req)).Items);
                i++;
            }

            albums.Items = CleanAlbums(albums.Items);
            foreach (var album in albums.Items)
            {
                if (album != null)
                {
                    var mappedAcronyms = await GetAcronymsFromSpotifyAlbum(new SpotifyRequest { SpotifyUrl = album.Id, SubredditId = request.SubredditId }, true);
                    foreach (var mappedAcronym in mappedAcronyms)
                    {
                        if (!acronyms.Any(x => x.AcronymName == mappedAcronym.AcronymName))
                            acronyms.Add(mappedAcronym);
                    }
                }
            }

            return acronyms;
        }

        public async Task<List<Acronym>>? GetAcronymsFromSpotifyAlbum(SpotifyRequest request, bool isId = false)
        {
            var client = await BuildClientAsync();
            var acronyms = new List<Acronym>();
            var req = new AlbumRequest { Market = "US" };

            var album = await client.Albums.Get(isId ? request.SpotifyUrl : GetIdFromUrl(request.SpotifyUrl), req);

            var mappedAlbum = await MapAlbumToAcronyms(album, request.SubredditId);
            if (mappedAlbum != null && !acronyms.Any(x => x.AcronymName == mappedAlbum?.AcronymName))
                acronyms.Add(mappedAlbum);


            if (album.Tracks.Items == null)
                throw new ArgumentNullException($"{nameof(album)} is null.");

            foreach (var track in album.Tracks.Items)
            {
                var mappedTracks = await MapTrackToAcronyms(track, album, request.SubredditId);
                if (mappedTracks != null)
                    foreach (var mappedTrack in mappedTracks)
                    {
                        if (!acronyms.Any(x => x.AcronymName == mappedTrack?.AcronymName))
                            acronyms.Add(mappedTrack);
                    }
            }

            return acronyms;
        }

        public async Task<List<Acronym>>? GetAcronymsFromSpotifyTrack(SpotifyRequest request)
        {
            var client = await BuildClientAsync();
            var req = new TrackRequest{ Market = "US" };

            var track = await client.Tracks.Get(GetIdFromUrl(request.SpotifyUrl), req);
            return await MapTrackToAcronyms(track, request.SubredditId);
        }

        private static async Task<SpotifyClient> BuildClientAsync()
        {
            var config = SpotifyClientConfig.CreateDefault();

            var request = new ClientCredentialsRequest("aa7dc193891148dfb4b9ae09ac0a1b36", "ca924f39653d479b9c0353eef21f11ad");
            var response = await new OAuthClient(config).RequestToken(request);

            return new SpotifyClient(config.WithToken(response.AccessToken));
        }

        private static string GetIdFromUrl(string url)
        {
            url = url.Substring(url.LastIndexOf("/") + 1);
            url = url.Substring(0, url.LastIndexOf('?'));
            return url;
        }

        private static List<SimpleAlbum>? CleanAlbums(List<SimpleAlbum>? albums)
        {
            var excludeAlbums = new List<SimpleAlbum>();
            string[] whitelist = { "taylor's version", "deluxe" };
            string[] blacklist = { "karaoke", "live", "playlist", "anniversary", "remix", "disney+" };
            string[] graylist = { "-", "(", ":" };

            foreach (var album in albums)
            {
                var name = album.Name.ToLower();

                if (blacklist.Any(name.Contains))
                    excludeAlbums.Add(album);
                if (whitelist.Any(name.Contains))
                    continue;
                if (graylist.Any(name.Contains))
                    excludeAlbums.Add(album);
            }

            return albums.Except(excludeAlbums).OrderBy(x => x.ReleaseDate).GroupBy(x => x.Name).Select(x => x.First()).ToList();
        }

        private async Task<Acronym>? MapAlbumToAcronyms(FullAlbum? album, string subredditId)
        {
            Acronym? acronym = null;

            if (album == null)
                throw new ArgumentNullException($"{nameof(album)} is null.");

            var acronymNames = GetAcronyms(album.Name);

            foreach (var acronymName in acronymNames)
            {
                if (ExcludedAcronyms.Contains(acronymName))
                    return null;

                if (subredditId == null && acronymName.Length < 5)
                    return null;
                else if (acronymName.Length < 3)
                    return null;

                var subreddit = await GetSubbredit(subredditId, acronymName);

                if (subreddit != null && await VerifyUniqueAcronymAsync(acronymName, subreddit?.Id))
                    acronym = new Acronym
                    {
                        AcronymName = acronymName,
                        AcronymType = AcronymType.Album,
                        AlbumName = album.Name,
                        ArtistName = string.Join(", ", album.Artists.Select(x => x.Name)),
                        Enabled = true,
                        Subreddit = subreddit,
                        TrackName = null,
                        YearReleased = album.ReleaseDatePrecision == "year" ? album.ReleaseDate : Convert.ToDateTime(album.ReleaseDate).Year.ToString(),
                    };
            }

            return acronym;
        }

        private async Task<List<Acronym>>? MapTrackToAcronyms(SimpleTrack? track, FullAlbum? album, string subredditId)
        {
            List<Acronym>? acronyms = new List<Acronym>();

            if (track == null)
                throw new ArgumentNullException($"{nameof(track)} is null.");
            if (album == null)
                throw new ArgumentNullException($"{nameof(album)} is null.");

            var acronymNames = GetAcronyms(track.Name);

            foreach (var acronymName in acronymNames) 
            {
                if (ExcludedAcronyms.Contains(acronymName))
                    return null;

                if (subredditId == null && acronymName.Length < 5)
                    return null;
                else if (acronymName.Length < 3)
                    return null;

                var subreddit = await GetSubbredit(subredditId, acronymName);

                if (subreddit != null && await VerifyUniqueAcronymAsync(acronymName, subreddit?.Id))
                    acronyms.Add(new Acronym
                    {
                        AcronymName = acronymName,
                        AcronymType = album.AlbumType == "single" ? AcronymType.Single : AcronymType.Track,
                        AlbumName = album.Name,
                        ArtistName = string.Join(", ", album.Artists.Select(x => x.Name)),
                        Enabled = true,
                        Subreddit = subreddit,
                        TrackName = track.Name,
                        YearReleased = album.ReleaseDatePrecision == "year" ? album.ReleaseDate : Convert.ToDateTime(album.ReleaseDate).Year.ToString(),
                    });
            }

            return acronyms;
        }

        private async Task<List<Acronym>>? MapTrackToAcronyms(FullTrack? track, string subredditId)
        {
            List<Acronym>? acronyms = new List<Acronym>();

            if (track == null)
                throw new ArgumentNullException($"{nameof(track)} is null.");

            var acronymNames = GetAcronyms(track.Name);

            foreach (var acronymName in acronymNames)
            {
                if (ExcludedAcronyms.Contains(acronymName))
                    return null;

                if (subredditId == null && acronymName.Length < 5)
                    return null;
                else if (acronymName.Length < 3)
                    return null;

                var subreddit = await GetSubbredit(subredditId, acronymName);

                if (subreddit != null && await VerifyUniqueAcronymAsync(acronymName, subreddit?.Id))
                    acronyms.Add(new Acronym
                    {
                        AcronymName = acronymName,
                        AcronymType = track.Album.AlbumType == "single" ? AcronymType.Single : AcronymType.Track,
                        AlbumName = track.Album.Name,
                        ArtistName = string.Join(", ", track.Artists.Select(x => x.Name)),
                        Enabled = true,
                        Subreddit = subreddit,
                        TrackName = track.Name,
                        YearReleased = track.Album.ReleaseDatePrecision == "year" ? track.Album.ReleaseDate : Convert.ToDateTime(track.Album.ReleaseDate).Year.ToString(),
                    });
            }

            return acronyms;
        }

        private static List<string> GetAcronyms(string name)
        {
            List<string> acronyms = new List<string>();

            // Remove everything after the first (
            var index = name.IndexOf('(');
            if (index >= 0)
                name = name.Substring(0, name.IndexOf('('));

            // Remove everything after the first -
            index = name.IndexOf('-');
            if (index >= 0)
                name = name.Substring(0, name.IndexOf('-'));

            // Remove all characters that aren't letters or numbers
            name = Regex.Replace(name, @"[^a-zA-Z0-9] -", "");

            name = name.Replace(".", "");
            name = name.Replace("‘", "");
            name = name.Replace("’", "");
            // Remove all beginning or trailing whitespace characters and uppercase the entire string.
            name = name.Trim().ToUpperInvariant();

            var acronym = string.Join(string.Empty,
                name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])
            );

            acronyms.Add(acronym);

            if (acronym.Contains('&'))
            {
                acronyms.Add(acronym.Replace('&', 'A'));
                acronyms.Add(acronym.Replace("&", ""));
            }

            if (acronym.Contains('/'))
                acronyms.Add(acronym.Replace("/", ""));

            return acronyms;
        }

        private async Task<Subreddit?> GetSubbredit(string? subredditId, string acronym)
        {
            if (subredditId == null || acronym.Length >= 5)
                return await _subredditRepository.GetByIdAsync("global");
            else
                return await _subredditRepository.GetByIdAsync(subredditId);
        }

        private async Task<bool> VerifyUniqueAcronymAsync(string acronym, string? subredditId)
        {
            if (subredditId == null)
                return true;

            var existingAcronyms = await _acronymRepository.GetAllBySubredditIdAsync(subredditId);

            if (existingAcronyms.Any(x => x.AcronymName == acronym))
                return false;

            return true;
        }
    }
}
