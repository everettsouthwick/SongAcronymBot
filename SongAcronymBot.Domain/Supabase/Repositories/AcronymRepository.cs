using SongAcronymBot.Domain.Supabase.Models;
using SongAcronymBot.Domain.Supabase.Services;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    public class AcronymRepository(
        ISupabaseService supabaseService,
        ISubredditRepository subredditRepository,
        ISubredditArtistRepository subredditArtistRepository,
        IArtistRepository artistRepository,
        IAlbumRepository albumRepository,
        ITrackRepository trackRepository) : BaseRepository<Acronym>(supabaseService), IAcronymRepository
    {
        private readonly ISubredditRepository _subredditRepository = subredditRepository;
        private readonly ISubredditArtistRepository _subredditArtistRepository = subredditArtistRepository;
        private readonly IArtistRepository _artistRepository = artistRepository;
        private readonly IAlbumRepository _albumRepository = albumRepository;
        private readonly ITrackRepository _trackRepository = trackRepository;

        public async Task<Acronym?> GetByAcronymTextAsync(string acronym)
        {
            var response = await GetQueryBuilder()
                .Filter("acronym", Operator.Equals, acronym)
                .Single();

            return response;
        }

        public async Task<Acronym?> GetByArtistAndAcronymTextAsync(Guid artistId, string acronym)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Filter("acronym", Operator.Equals, acronym)
                .Single();

            return response;
        }

        public async Task<List<Acronym>> GetByArtistIdAsync(Guid artistId)
        {
            var response = await GetQueryBuilder()
                .Filter("artist_id", Operator.Equals, artistId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<Acronym>> GetByAlbumIdAsync(Guid albumId)
        {
            var response = await GetQueryBuilder()
                .Filter("album_id", Operator.Equals, albumId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<Acronym>> GetByTrackIdAsync(Guid trackId)
        {
            var response = await GetQueryBuilder()
                .Filter("track_id", Operator.Equals, trackId.ToString())
                .Get();

            return response.Models;
        }

        public async Task<List<Acronym>> GetByTypeAsync(AcronymType type)
        {
            var response = await GetQueryBuilder()
                .Filter("acronym_type", Operator.Equals, type.ToString().ToLowerInvariant())
                .Get();

            return response.Models;
        }

        public async Task<List<Acronym>> GetActiveAcronymsAsync()
        {
            var response = await GetQueryBuilder()
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            return response.Models;
        }

        public async Task<List<EnrichedAcronym>> GetEnrichedAcronymsBySubredditNameAsync(string subredditName)
        {
            // 1. Get the subreddit by name
            var subreddit = await _subredditRepository.GetByNameAsync(subredditName);
            if (subreddit == null || !subreddit.IsActive)
            {
                return [];
            }

            // 2. Get all artist IDs linked to this subreddit
            var subredditArtists = await _subredditArtistRepository.GetBySubredditIdAsync(subreddit.Id);
            if (subredditArtists.Count == 0)
            {
                return [];
            }

            var artistIds = subredditArtists.Select(sa => sa.ArtistId).ToHashSet();

            // 3. Get all acronyms for these artists
            var allAcronymsResponse = await GetQueryBuilder()
                .Filter("artist_id", Operator.In, artistIds.Select(id => id.ToString()).ToList())
                .Filter("is_active", Operator.Equals, "true")
                .Get();
            
            var allAcronyms = allAcronymsResponse.Models;

            // 4. Filter by min acronym length for this subreddit
            allAcronyms = allAcronyms
                .Where(a => a.AcronymText.Length >= subreddit.MinAcronymLength)
                .ToList();

            // 5. Enrich the acronyms with artist/album/track data
            return await EnrichAcronymsAsync(allAcronyms);
        }

        public async Task<List<EnrichedAcronym>> GetEnrichedAcronymsByTextAsync(string acronymText)
        {
            // Get all acronyms matching this text (case-insensitive)
            var response = await GetQueryBuilder()
                .Filter("acronym", Operator.Equals, acronymText.ToUpperInvariant())
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            return await EnrichAcronymsAsync(response.Models);
        }

        private async Task<List<EnrichedAcronym>> EnrichAcronymsAsync(List<Acronym> acronyms)
        {
            if (acronyms.Count == 0)
            {
                return [];
            }

            // Collect all unique IDs for batch fetching
            var artistIds = acronyms.Where(a => a.ArtistId.HasValue).Select(a => a.ArtistId!.Value).Distinct().ToList();
            var albumIds = acronyms.Where(a => a.AlbumId.HasValue).Select(a => a.AlbumId!.Value).Distinct().ToList();
            var trackIds = acronyms.Where(a => a.TrackId.HasValue).Select(a => a.TrackId!.Value).Distinct().ToList();

            // Fetch related entities
            var artists = new Dictionary<Guid, Artist>();
            if (artistIds.Count > 0)
            {
                var response = await _supabaseService.GetClient().From<Artist>()
                    .Filter("id", Operator.In, artistIds.Select(id => id.ToString()).ToList())
                    .Get();
                foreach (var artist in response.Models) artists[artist.Id] = artist;
            }

            var albums = new Dictionary<Guid, Album>();
            if (albumIds.Count > 0)
            {
                var response = await _supabaseService.GetClient().From<Album>()
                    .Filter("id", Operator.In, albumIds.Select(id => id.ToString()).ToList())
                    .Get();
                foreach (var album in response.Models) albums[album.Id] = album;
            }

            var tracks = new Dictionary<Guid, Track>();
            if (trackIds.Count > 0)
            {
                var response = await _supabaseService.GetClient().From<Track>()
                    .Filter("id", Operator.In, trackIds.Select(id => id.ToString()).ToList())
                    .Get();
                foreach (var track in response.Models) tracks[track.Id] = track;
            }

            // Build enriched acronyms
            var enrichedAcronyms = new List<EnrichedAcronym>();
            foreach (var acronym in acronyms)
            {
                var enriched = new EnrichedAcronym
                {
                    Id = acronym.Id,
                    AcronymText = acronym.AcronymText,
                    AcronymType = ParseAcronymType(acronym.AcronymType),
                    IsActive = acronym.IsActive,
                    ArtistId = acronym.ArtistId,
                    AlbumId = acronym.AlbumId,
                    TrackId = acronym.TrackId
                };

                // Add artist info
                if (acronym.ArtistId.HasValue && artists.TryGetValue(acronym.ArtistId.Value, out var artist))
                {
                    enriched.ArtistName = artist.Name;
                    enriched.ArtistSlug = artist.Slug;
                }

                // Add album info
                if (acronym.AlbumId.HasValue && albums.TryGetValue(acronym.AlbumId.Value, out var album))
                {
                    enriched.AlbumName = album.Name;
                    enriched.YearReleased = album.YearReleased;
                    enriched.AlbumSlug = album.Slug;
                }

                // Add track info
                if (acronym.TrackId.HasValue && tracks.TryGetValue(acronym.TrackId.Value, out var track))
                {
                    enriched.TrackName = track.Name;
                    enriched.IsSingle = track.IsSingle;

                    // If track has album but acronym doesn't, get album from track
                    if (track.AlbumId.HasValue && !acronym.AlbumId.HasValue)
                    {
                        if (!albums.TryGetValue(track.AlbumId.Value, out var trackAlbum))
                        {
                            trackAlbum = await _albumRepository.GetByIdAsync(track.AlbumId.Value);
                        }
                        if (trackAlbum != null)
                        {
                            enriched.AlbumId = trackAlbum.Id;
                            enriched.AlbumName = trackAlbum.Name;
                            enriched.YearReleased = trackAlbum.YearReleased;
                            enriched.AlbumSlug = trackAlbum.Slug;
                        }
                    }
                }

                enrichedAcronyms.Add(enriched);
            }

            return enrichedAcronyms;
        }

        private static AcronymType ParseAcronymType(string type)
        {
            return type.ToLowerInvariant() switch
            {
                "artist" => AcronymType.Artist,
                "album" => AcronymType.Album,
                "track" => AcronymType.Track,
                "single" => AcronymType.Single,
                _ => AcronymType.Track
            };
        }
    }
}
