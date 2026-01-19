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
            return $"{body}\n---\n\n^[/u/{author}](/u/{author}) ^(can reply with \"delete\" to remove comment.)";
        }

        /// <inheritdoc/>
        public string BuildReplyBody(IEnumerable<AcronymMatch> matches, string author)
        {
            var replyBody = "";

            // Group matches by acronym text to consolidate duplicates
            var groupedMatches = matches
                .GroupBy(m => m.Acronym)
                .OrderBy(g => g.Min(m => m.Position));

            foreach (var group in groupedMatches)
            {
                var matchList = group.ToList();
                if (matchList.Count == 1)
                {
                    // Single match - use original CommentBody
                    replyBody += matchList[0].CommentBody;
                }
                else
                {
                    // Multiple matches for same acronym - consolidate with "or"
                    var descriptions = string.Join(" or ", matchList.Select(m => m.MatchDescription));
                    replyBody += $"- {group.Key} could mean {descriptions}.\n";
                }
            }

            return FormatReplyBodyWithFooter(replyBody, author);
        }
    }
}
