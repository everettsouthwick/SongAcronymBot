using Reddit;
using Reddit.Controllers;
using SongAcronymBot.Core.Model;

namespace SongAcronymBot.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for processing Reddit comments.
    /// </summary>
    public interface ICommentProcessor
    {
        /// <summary>
        /// Processes a Reddit comment for potential acronym matches.
        /// </summary>
        /// <param name="reddit">The Reddit client.</param>
        /// <param name="comment">The comment to process.</param>
        Task ProcessCommentAsync(RedditClient reddit, Comment comment);

        /// <summary>
        /// Checks if a comment is repliable (not from self, not opted out, not too old).
        /// </summary>
        /// <param name="comment">The comment to check.</param>
        /// <returns>True if the comment can be replied to.</returns>
        bool IsRepliable(Comment comment);

        /// <summary>
        /// Finds all matching acronyms in a comment.
        /// </summary>
        /// <param name="comment">The Reddit comment.</param>
        /// <returns>List of matched acronyms.</returns>
        Task<List<AcronymMatch>> FindAcronymsAsync(Comment comment);
    }
}
