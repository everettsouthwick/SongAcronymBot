namespace SongAcronymBot.Core.Services.Interfaces
{
    /// <summary>
    /// Formats promotional messages for Reddit display.
    /// </summary>
    public interface IPromotionalMessageFormatter
    {
        /// <summary>
        /// Formats text with ^ prefix on each word for Reddit small/superscript text.
        /// </summary>
        /// <param name="text">The text to format.</param>
        /// <returns>The formatted text with ^ prefixes.</returns>
        string FormatAsSmallText(string text);

        /// <summary>
        /// Formats a complete promotional message with URL for appending to comments.
        /// </summary>
        /// <param name="messageText">The promotional message text.</param>
        /// <param name="url">The URL to link to.</param>
        /// <returns>The formatted promotional message ready to append to a comment.</returns>
        string FormatPromotionalMessage(string messageText, string url);

        /// <summary>
        /// Checks if a comment body already contains promotional content.
        /// </summary>
        /// <param name="commentBody">The comment body to check.</param>
        /// <param name="url">The promotional URL to look for.</param>
        /// <returns>True if the comment already contains promotional content.</returns>
        bool ContainsPromotionalContent(string commentBody, string url);
    }
}
