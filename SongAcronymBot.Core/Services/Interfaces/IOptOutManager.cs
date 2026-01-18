namespace SongAcronymBot.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for managing user opt-in/opt-out state.
    /// </summary>
    public interface IOptOutManager
    {
        /// <summary>
        /// Checks if a user has opted out of receiving automatic replies.
        /// </summary>
        /// <param name="username">The Reddit username.</param>
        /// <returns>True if the user has opted out.</returns>
        bool IsOptedOut(string username);

        /// <summary>
        /// Adds a user to the opted-out list.
        /// </summary>
        /// <param name="username">The Reddit username.</param>
        Task AddOptedOutRedditorAsync(string username);

        /// <summary>
        /// Removes a user from the opted-out list.
        /// </summary>
        /// <param name="username">The Reddit username.</param>
        Task RemoveOptedOutRedditorAsync(string username);

        /// <summary>
        /// Refreshes the cached list of opted-out users from the database.
        /// </summary>
        Task RefreshOptedOutUsersAsync();

        /// <summary>
        /// Gets the count of opted-out users.
        /// </summary>
        int OptedOutCount { get; }
    }
}
