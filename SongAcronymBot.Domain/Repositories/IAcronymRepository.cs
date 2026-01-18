using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Repositories
{
    public interface IAcronymRepository : IBaseRepository<Acronym>
    {
        Task<Acronym?> GetByAcronymTextAsync(string acronym);
        Task<Acronym?> GetByArtistAndAcronymTextAsync(Guid artistId, string acronym);
        Task<List<Acronym>> GetByArtistIdAsync(Guid artistId);
        Task<List<Acronym>> GetByAlbumIdAsync(Guid albumId);
        Task<List<Acronym>> GetByTrackIdAsync(Guid trackId);
        Task<List<Acronym>> GetByTypeAsync(AcronymType type);
        Task<List<Acronym>> GetActiveAcronymsAsync();

        // New methods for RedditService
        Task<List<EnrichedAcronym>> GetEnrichedAcronymsBySubredditNameAsync(string subredditName);
        Task<List<EnrichedAcronym>> GetEnrichedAcronymsByTextAsync(string acronymText);
    }
}
