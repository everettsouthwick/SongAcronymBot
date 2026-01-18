using Reddit;
using SongAcronymBot.Core.Model;

namespace SongAcronymBot.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for processing Reddit messages/summons.
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// Processes a Reddit message (username mention or reply).
        /// </summary>
        /// <param name="reddit">The Reddit client.</param>
        /// <param name="message">The message to process.</param>
        Task ProcessMessageAsync(RedditClient reddit, Reddit.Things.Message message);

        /// <summary>
        /// Finds acronyms requested in a summon message.
        /// </summary>
        /// <param name="message">The Reddit message.</param>
        /// <returns>List of matched acronyms.</returns>
        Task<List<AcronymMatch>> FindAcronymsAsync(Reddit.Things.Message message);

        /// <summary>
        /// Parses acronym queries from a mention message.
        /// </summary>
        /// <param name="message">The Reddit message.</param>
        /// <returns>List of acronym text to query.</returns>
        List<string> ParseAcronymsFromMention(Reddit.Things.Message message);
    }
}
