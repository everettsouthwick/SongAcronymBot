using SongAcronymBot.Core.Services.Interfaces;

namespace SongAcronymBot.Core.Services
{
    /// <summary>
    /// Formats promotional messages for Reddit display.
    /// </summary>
    public class PromotionalMessageFormatter : IPromotionalMessageFormatter
    {
        /// <inheritdoc/>
        public string FormatAsSmallText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var formattedText = string.Join(" ", words.Select(w => $"^{w}"));
            return formattedText + " ";
        }

        /// <inheritdoc/>
        public string FormatPromotionalMessage(string messageText, string url)
        {
            ArgumentNullException.ThrowIfNull(messageText);
            ArgumentNullException.ThrowIfNull(url);

            var formattedText = FormatAsSmallText(messageText);
            return $"\n\n[{formattedText}]({url})";
        }

        /// <inheritdoc/>
        public bool ContainsPromotionalContent(string commentBody, string url)
        {
            if (string.IsNullOrEmpty(commentBody) || string.IsNullOrEmpty(url))
            {
                return false;
            }

            return commentBody.Contains(url, StringComparison.OrdinalIgnoreCase);
        }
    }
}
