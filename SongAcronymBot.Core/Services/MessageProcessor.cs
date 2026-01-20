using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Exceptions;
using SongAcronymBot.Core.Model;
using SongAcronymBot.Core.Services.Interfaces;
using SongAcronymBot.Domain.Repositories.Interfaces;

namespace SongAcronymBot.Core.Services
{
    /// <summary>
    /// Handles processing of Reddit messages and summons.
    /// </summary>
    public class MessageProcessor(
        IAcronymRepository acronymRepository,
        IOptOutManager optOutManager,
        IReplyFormatter replyFormatter,
        ILogger<MessageProcessor> logger) : IMessageProcessor
    {
        private readonly IAcronymRepository _acronymRepository = acronymRepository ?? throw new ArgumentNullException(nameof(acronymRepository));
        private readonly IOptOutManager _optOutManager = optOutManager ?? throw new ArgumentNullException(nameof(optOutManager));
        private readonly IReplyFormatter _replyFormatter = replyFormatter ?? throw new ArgumentNullException(nameof(replyFormatter));
        private readonly ILogger<MessageProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <inheritdoc/>
        public async Task ProcessMessageAsync(RedditClient reddit, Reddit.Things.Message message)
        {
            if (await IsBadBotAsync(reddit, message))
            {
                return;
            }

            if (await IsDeleteAsync(reddit, message))
            {
                return;
            }

            if (IsNotSummon(message))
            {
                return;
            }

            var matches = await FindAcronymsAsync(message);

            if (matches.Count == 0)
            {
                _logger.LogTrace("No acronym matches for summon from u/{Author}", message.Author);
                return;
            }

            var replyBody = _replyFormatter.BuildReplyBody(matches, message.Author);

            _logger.LogDebug("Generated reply: {ReplyBody}", replyBody);

            try
            {
                var comment = reddit.Comment($"t1_{message.Id}").About();
                await comment.ReplyAsync(replyBody);
                _logger.LogInformation("Replied to summon in r/{Subreddit} by u/{Author}", comment.Subreddit, message.Author);
            }
            catch (RedditForbiddenException ex)
            {
                _logger.LogError(ex, "Failed to reply to summon from u/{Author}", message.Author);
            }
        }

        /// <inheritdoc/>
        public async Task<List<AcronymMatch>> FindAcronymsAsync(Reddit.Things.Message message)
        {
            var matches = new List<AcronymMatch>();

            var acronymsToQuery = ParseAcronymsFromMention(message);

            for (int i = 0; i < acronymsToQuery.Count; i++)
            {
                var query = acronymsToQuery[i];
                var results = await _acronymRepository.GetEnrichedAcronymsByTextAsync(query);
                var acronyms = results.GroupBy(x => x.ArtistName).Select(x => x.First()).ToList();

                if (acronyms.Count > 0)
                {
                    foreach (var acronym in acronyms)
                    {
                        matches.Add(new AcronymMatch(acronym, i + 1));
                    }
                }
            }

            return matches;
        }

        /// <inheritdoc/>
        public List<string> ParseAcronymsFromMention(Reddit.Things.Message message)
        {
            var acronymsToQuery = new List<string>();

            var words = message.Body.ToUpper().Split(' ');

            if (!words[0].Contains("SONGACRONYMBOT"))
            {
                return acronymsToQuery;
            }

            foreach (var word in words)
            {
                if (word.Contains("SONGACRONYMBOT"))
                {
                    continue;
                }

                acronymsToQuery.Add(word.Trim());
            }

            return acronymsToQuery;
        }

        private async Task<bool> IsBadBotAsync(RedditClient reddit, Reddit.Things.Message message)
        {
            if (message.Subject != "comment reply" || !message.Body.Equals("bad bot", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            var parent = reddit.Comment(message.ParentId).About();

            if (parent.Author.Equals("songacronymbot", StringComparison.CurrentCultureIgnoreCase))
            {
                if (parent.UpVotes < 5)
                {
                    await parent.DeleteAsync();
                    _logger.LogDebug("Deleted low-score comment (score: {Score}) via 'bad bot' from u/{Author}", parent.UpVotes, message.Author);
                }

                await _optOutManager.AddOptedOutRedditorAsync(message.Author);
                _logger.LogInformation("Processed 'bad bot' command from u/{Author}", message.Author);
                return true;
            }

            return false;
        }

        private async Task<bool> IsDeleteAsync(RedditClient reddit, Reddit.Things.Message message)
        {
            if (message.Subject != "comment reply" || !message.Body.Equals("delete", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            var parent = reddit.Comment(message.ParentId).About();

            if (!parent.Author.Equals("songacronymbot", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if (parent.Body.Contains(message.Author, StringComparison.CurrentCultureIgnoreCase))
            {
                await parent.DeleteAsync();
                await _optOutManager.AddOptedOutRedditorAsync(message.Author);
                _logger.LogInformation("Processed 'delete' command from u/{Author} in r/{Subreddit}", message.Author, parent.Subreddit);
                return true;
            }

            return false;
        }

        private bool IsNotSummon(Reddit.Things.Message message)
        {
            if (message.Subject == "username mention" && message.WasComment)
            {
                return false;
            }

            _logger.LogTrace("Skipping non-summon message from u/{Author} (subject: {Subject})", message.Author, message.Subject);
            return true;
        }
    }
}
