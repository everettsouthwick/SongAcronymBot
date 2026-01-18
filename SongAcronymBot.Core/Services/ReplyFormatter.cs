using SongAcronymBot.Core.Model;
using SongAcronymBot.Core.Services.Interfaces;

namespace SongAcronymBot.Core.Services
{
    /// <summary>
    /// Handles formatting of Reddit reply bodies.
    /// </summary>
    public class ReplyFormatter : IReplyFormatter
    {
        /// <inheritdoc/>
        public string FormatReplyBodyWithFooter(string body, string author)
        {
            return $"{body}\n---\n\n^[/u/{author}](/u/{author}) ^(can reply with \"delete\" to remove comment. |) ^[/r/songacronymbot](/r/songacronymbot) ^(for feedback.)";
        }

        /// <inheritdoc/>
        public string BuildReplyBody(IEnumerable<AcronymMatch> matches, string author)
        {
            var replyBody = "";
            foreach (var match in matches)
            {
                replyBody += match.CommentBody;
            }
            return FormatReplyBodyWithFooter(replyBody, author);
        }
    }
}
