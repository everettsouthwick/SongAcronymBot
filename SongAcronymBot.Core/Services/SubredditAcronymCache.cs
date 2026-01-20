using Microsoft.Extensions.Logging;
using SongAcronymBot.Core.Services.Interfaces;
using SongAcronymBot.Domain.Repositories.Interfaces;
using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Core.Services
{
    /// <summary>
    /// Manages cached acronyms per subreddit with time-based invalidation.
    /// </summary>
    public class SubredditAcronymCache(
        IAcronymRepository acronymRepository,
        ILogger<SubredditAcronymCache> logger) : ISubredditAcronymCache
    {
        private readonly IAcronymRepository _acronymRepository = acronymRepository ?? throw new ArgumentNullException(nameof(acronymRepository));
        private readonly ILogger<SubredditAcronymCache> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private readonly Dictionary<string, List<EnrichedAcronym>> _cache = [];
        private readonly Dictionary<string, DateTime> _lastUpdate = [];
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromHours(24);

        /// <inheritdoc/>
        public async Task<List<EnrichedAcronym>> GetAcronymsAsync(string subredditName)
        {
            if (!_cache.TryGetValue(subredditName, out List<EnrichedAcronym>? value) ||
                DateTime.UtcNow - _lastUpdate[subredditName] > _cacheTimeout)
            {
                try
                {
                    value = await _acronymRepository.GetEnrichedAcronymsBySubredditNameAsync(subredditName);

                    if (value.Count == 0)
                    {
                        _logger.LogWarning("Cache refresh for r/{Subreddit} found 0 acronyms", subredditName);
                    }
                    else
                    {
                        _logger.LogDebug("Cache refresh for r/{Subreddit} found {Count} acronyms", subredditName, value.Count);
                    }

                    _cache[subredditName] = value;
                    _lastUpdate[subredditName] = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh acronym cache for r/{Subreddit}", subredditName);
                    value = [];
                }
            }

            return value;
        }
    }
}
