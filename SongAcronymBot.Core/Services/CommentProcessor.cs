using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using SongAcronymBot.Core.Model;
using SongAcronymBot.Core.Services.Interfaces;

namespace SongAcronymBot.Core.Services
{
    /// <summary>
    /// Handles processing of Reddit comments for acronym matching.
    /// </summary>
    public class CommentProcessor(
        ISubredditAcronymCache acronymCache,
        IAcronymMatcher acronymMatcher,
        IOptOutManager optOutManager,
        IReplyFormatter replyFormatter,
        ILogger<CommentProcessor> logger) : ICommentProcessor
    {
        private readonly ISubredditAcronymCache _acronymCache = acronymCache ?? throw new ArgumentNullException(nameof(acronymCache));
        private readonly IAcronymMatcher _acronymMatcher = acronymMatcher ?? throw new ArgumentNullException(nameof(acronymMatcher));
        private readonly IOptOutManager _optOutManager = optOutManager ?? throw new ArgumentNullException(nameof(optOutManager));
        private readonly IReplyFormatter _replyFormatter = replyFormatter ?? throw new ArgumentNullException(nameof(replyFormatter));
        private readonly ILogger<CommentProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private const string OptInOutPostId = "j9yq8q";

        /// <inheritdoc/>
        public async Task ProcessCommentAsync(RedditClient reddit, Comment comment)
        {
            if (!IsRepliable(comment))
            {
                return;
            }

            if (await IsOptInOrOptOutAsync(reddit, comment))
            {
                return;
            }

            var matches = await FindAcronymsAsync(comment);

            if (matches.Count == 0)
            {
                return;
            }

            var replyBody = _replyFormatter.BuildReplyBody(matches, comment.Author);

            _logger.LogDebug("Reply body: {ReplyBody}", replyBody);

            try
            {
                await comment.ReplyAsync(replyBody);
            }
            catch (RedditForbiddenException ex)
            {
                _logger.LogWarning(ex, "Failed to reply - forbidden", ex.Message);
            }
            catch (RedditControllerException ex)
            {
                _logger.LogWarning(ex, "Failed to reply - controller error: {Message}", ex.Message);
            }
        }

        /// <inheritdoc/>
        public bool IsRepliable(Comment comment)
        {
            // Do not reply to our own submissions
            if (comment.Author.Equals("songacronymbot", StringComparison.CurrentCultureIgnoreCase))
            {
                _logger.LogTrace("Skipping comment from self");
                return false;
            }

            // Do not reply to submissions by someone who has disabled us
            if (_optOutManager.IsOptedOut(comment.Author))
            {
                _logger.LogTrace("Skipping comment from disabled user: {Author}", comment.Author);
                return false;
            }

            // Do not reply to comments older than 24 hours
            var commentAge = DateTimeOffset.UtcNow - comment.Created;
            if (commentAge.TotalHours > 24)
            {
                _logger.LogTrace("Skipping comment older than 24 hours");
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<List<AcronymMatch>> FindAcronymsAsync(Comment comment)
        {
            var acronyms = await _acronymCache.GetAcronymsAsync(comment.Subreddit);
            return _acronymMatcher.FindMatches(comment, acronyms);
        }

        private async Task<bool> IsOptInOrOptOutAsync(RedditClient reddit, Comment comment)
        {
            if (comment.Root.Id.Equals(OptInOutPostId, StringComparison.CurrentCultureIgnoreCase))
            {
                if (comment.Body.Equals("optout", StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogInformation("User {Author} opted out", comment.Author);
                    await _optOutManager.AddOptedOutRedditorAsync(comment.Author);
                    try
                    {
                        await comment.ReplyAsync(_replyFormatter.FormatReplyBodyWithFooter("- Your account has been disabled from receiving automatic replies.\n", comment.Author));
                    }
                    catch (RedditForbiddenException ex)
                    {
                        _logger.LogWarning(ex, "Failed to reply to opt-out");
                    }
                    return true;
                }
                else if (comment.Body.Equals("optin", StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogInformation("User {Author} opted in", comment.Author);
                    await _optOutManager.RemoveOptedOutRedditorAsync(comment.Author);
                    try
                    {
                        await comment.ReplyAsync(_replyFormatter.FormatReplyBodyWithFooter("- Your account has been enabled for receiving automatic replies.\n", comment.Author));
                    }
                    catch (RedditForbiddenException ex)
                    {
                        _logger.LogWarning(ex, "Failed to reply to opt-in");
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
