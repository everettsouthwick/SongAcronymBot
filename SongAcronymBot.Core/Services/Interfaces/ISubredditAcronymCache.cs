using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for caching subreddit acronyms with time-based invalidation.
    /// </summary>
    public interface ISubredditAcronymCache
    {
        /// <summary>
        /// Gets the cached acronyms for a subreddit, refreshing if needed.
        /// </summary>
        /// <param name="subredditName">The subreddit name.</param>
        /// <returns>List of enriched acronyms applicable to the subreddit.</returns>
        Task<List<EnrichedAcronym>> GetAcronymsAsync(string subredditName);
    }
}
