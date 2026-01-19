namespace SongAcronymBot.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for formatting Reddit reply bodies.
    /// </summary>
    public interface IReplyFormatter
    {
        /// <summary>
        /// Formats a reply body with the standard footer.
        /// </summary>
        /// <param name="body">The main body content.</param>
        /// <param name="author">The username of the comment/message author.</param>
        /// <returns>The formatted reply body with footer.</returns>
        string FormatReplyBodyWithFooter(string body, string author);

        /// <summary>
        /// Builds a reply body from a list of acronym matches.
        /// </summary>
        /// <param name="matches">The matched acronyms.</param>
        /// <param name="author">The username of the comment/message author.</param>
        /// <returns>The complete formatted reply body.</returns>
        string BuildReplyBody(IEnumerable<Model.AcronymMatch> matches, string author);
    }
}
