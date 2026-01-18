using Microsoft.Extensions.Logging;
using SongAcronymBot.Core.Services.Interfaces;
using SongAcronymBot.Domain.Repositories.Interfaces;
using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Core.Services
{
    /// <summary>
    /// Manages user opt-in/opt-out state for receiving automatic replies.
    /// </summary>
    public class OptOutManager(
        IOptedOutRedditorRepository optedOutRedditorRepository,
        ILogger<OptOutManager> logger) : IOptOutManager
    {
        private readonly IOptedOutRedditorRepository _optedOutRedditorRepository = optedOutRedditorRepository ?? throw new ArgumentNullException(nameof(optedOutRedditorRepository));
        private readonly ILogger<OptOutManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private volatile HashSet<string> _disabledRedditors = null!;

        /// <inheritdoc/>
        public int OptedOutCount => _disabledRedditors?.Count ?? 0;

        /// <inheritdoc/>
        public bool IsOptedOut(string username)
        {
            return _disabledRedditors?.Contains(username) ?? false;
        }

        /// <inheritdoc/>
        public async Task AddOptedOutRedditorAsync(string username)
        {
            var existingRedditor = await _optedOutRedditorRepository.GetByUsernameAsync(username);
            if (existingRedditor != null)
            {
                return; // Already opted out
            }

            var optedOutRedditor = new OptedOutRedditor
            {
                Id = Guid.NewGuid(),
                Username = username,
                OptedOutAt = DateTime.UtcNow
            };
            await _optedOutRedditorRepository.CreateAsync(optedOutRedditor);
            await RefreshOptedOutUsersAsync();
        }

        /// <inheritdoc/>
        public async Task RemoveOptedOutRedditorAsync(string username)
        {
            var existingRedditor = await _optedOutRedditorRepository.GetByUsernameAsync(username);
            if (existingRedditor == null)
            {
                return; // Not opted out
            }

            await _optedOutRedditorRepository.DeleteAsync(existingRedditor.Id);
            await RefreshOptedOutUsersAsync();
        }

        /// <inheritdoc/>
        public async Task RefreshOptedOutUsersAsync()
        {
            _disabledRedditors = await _optedOutRedditorRepository.GetAllUsernamesAsync();
            _logger.LogDebug("Retrieved {Count} opted-out redditors", _disabledRedditors.Count);
        }
    }
}
