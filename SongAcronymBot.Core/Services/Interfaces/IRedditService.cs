using Reddit;

namespace SongAcronymBot.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for the main Reddit bot service.
    /// </summary>
    public interface IRedditService
    {
        /// <summary>
        /// Starts the Reddit bot and begins monitoring for comments and messages.
        /// </summary>
        /// <param name="reddit">The Reddit client instance.</param>
        Task StartAsync(RedditClient reddit);
    }
}
