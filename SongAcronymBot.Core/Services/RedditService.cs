using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using Reddit.Exceptions;
using SongAcronymBot.Core.Services.Interfaces;
using SongAcronymBot.Domain.Repositories.Interfaces;

namespace SongAcronymBot.Core.Services
{
    /// <summary>
    /// Main Reddit bot service that orchestrates message and comment processing.
    /// </summary>
    public class RedditService(
        IMessageProcessor messageProcessor,
        ICommentProcessor commentProcessor,
        IOptOutManager optOutManager,
        IPromotionalMessageFormatter promotionalMessageFormatter,
        IPromotionalMessageRepository promotionalMessageRepository,
        ISubredditRepository subredditRepository,
        ILogger<RedditService> logger) : IRedditService
    {
        private readonly IMessageProcessor _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        private readonly ICommentProcessor _commentProcessor = commentProcessor ?? throw new ArgumentNullException(nameof(commentProcessor));
        private readonly IOptOutManager _optOutManager = optOutManager ?? throw new ArgumentNullException(nameof(optOutManager));
        private readonly IPromotionalMessageFormatter _promotionalMessageFormatter = promotionalMessageFormatter ?? throw new ArgumentNullException(nameof(promotionalMessageFormatter));
        private readonly IPromotionalMessageRepository _promotionalMessageRepository = promotionalMessageRepository ?? throw new ArgumentNullException(nameof(promotionalMessageRepository));
        private readonly ISubredditRepository _subredditRepository = subredditRepository ?? throw new ArgumentNullException(nameof(subredditRepository));
        private readonly ILogger<RedditService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private RedditClient _reddit = null!;
        private System.Timers.Timer _commentCheckTimer = null!;

        /// <inheritdoc/>
        public async Task StartAsync(RedditClient reddit)
        {
            ArgumentNullException.ThrowIfNull(reddit);

            _reddit = reddit;

            await _optOutManager.RefreshOptedOutUsersAsync();

            try
            {
                // Monitor our new unread messages for mentions
                reddit.Account.Messages.GetMessagesUnread();
                reddit.Account.Messages.MonitorUnread();
                reddit.Account.Messages.UnreadUpdated += Messages_UnreadUpdated;
                reddit.Account.Me.GetCommentHistory();
                reddit.Account.Me.MonitorCommentHistory();
                reddit.Account.Me.CommentHistoryUpdated += Me_CommentHistoryUpdated;

                // Set up timer to check comments every hour for promotional message editing
                await CheckRecentComments();
                _commentCheckTimer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
                _commentCheckTimer.Elapsed += async (s, e) => await CheckRecentComments();
                _commentCheckTimer.Start();

                // Monitor all tracked subreddits from the database
                var activeSubreddits = await _subredditRepository.GetActiveSubredditsAsync();
                var subredditNames = activeSubreddits.Select(s => s.Name).ToList();

                if (subredditNames.Count == 0)
                {
                    _logger.LogWarning("No active subreddits found in database");
                    return;
                }

                var subredditString = string.Join("+", subredditNames);
                var trackedSubreddits = reddit.Subreddit(subredditString);

                _logger.LogInformation("Monitoring {Count} subreddits", subredditNames.Count);

                trackedSubreddits.Comments.MonitorNew();
                trackedSubreddits.Comments.NewUpdated += Comments_NewUpdated;
            }
            catch (Exception ex) when (ex is RedditForbiddenException or RedditBadGatewayException)
            {
                _logger.LogError(ex, "Failed to start Reddit service");
                throw;
            }
        }

        private static readonly Random _random = new();

        private async Task CheckRecentComments()
        {
            try
            {
                // 1% chance to edit a comment this hour
                if (_random.Next(100) >= 1)
                {
                    _logger.LogDebug("Skipping promotional message check (99% chance)");
                    return;
                }

                var comments = _reddit.Account.Me.GetCommentHistory(limit: 50);

                // Filter to comments between 24 and 25 hours old
                var eligibleComments = comments.Where(c =>
                {
                    var age = DateTimeOffset.UtcNow - c.Created;
                    return age.TotalHours >= 24 && age.TotalHours < 25;
                }).ToList();

                if (eligibleComments.Count == 0)
                {
                    _logger.LogDebug("No comments found in 24-25 hour window");
                    return;
                }

                // Get a random promotional message from the database
                var promoMessage = await _promotionalMessageRepository.GetRandomActiveMessageAsync();
                if (promoMessage == null)
                {
                    _logger.LogDebug("No active promotional messages in database");
                    return;
                }

                // Find a comment that hasn't already been edited with promotional text
                foreach (var comment in eligibleComments)
                {
                    try
                    {
                        // Format the promotional message and append to comment
                        var promoText = _promotionalMessageFormatter.FormatPromotionalMessage(promoMessage.MessageText, promoMessage.Url);
                        var newBody = comment.Body + promoText;
                        await comment.EditAsync(newBody);

                        _logger.LogInformation("Added promotional message to comment {CommentId}", comment.Id);

                        // Only edit one comment per check
                        return;
                    }
                    catch (RedditForbiddenException ex)
                    {
                        _logger.LogWarning(ex, "Failed to edit comment {CommentId}", comment.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check recent comments for promotional messages");
            }
        }

        #region Event Handlers

        private async void Messages_UnreadUpdated(object? sender, MessagesUpdateEventArgs e)
        {
            foreach (var message in e.Added)
            {
                _logger.LogTrace("New unread message from {Author}: {Body}", message.Author, message.Body);
                try
                {
                    await _messageProcessor.ProcessMessageAsync(_reddit, message);
                }
                catch (RedditForbiddenException ex)
                {
                    _logger.LogWarning(ex, "Failed to process message");
                }
            }
        }

        private async void Comments_NewUpdated(object? sender, CommentsUpdateEventArgs e)
        {
            foreach (var comment in e.Added)
            {
                _logger.LogTrace("New comment in {Subreddit}: {Title}", comment.Subreddit, comment.Root.Title);
                try
                {
                    await _commentProcessor.ProcessCommentAsync(_reddit, comment);
                }
                catch (RedditForbiddenException ex)
                {
                    _logger.LogWarning(ex, "Failed to process comment");
                }
                catch (RedditException ex) when (ex.Message.Contains("TooManyRequests"))
                {
                    _logger.LogWarning(ex, "Rate limited by Reddit API");
                }
            }
        }

        private async void Me_CommentHistoryUpdated(object? sender, CommentsUpdateEventArgs e)
        {
            _logger.LogDebug("New comment history activity");
            await ProcessCommentHistoryAsync(e.NewComments);
        }

        private async Task ProcessCommentHistoryAsync(List<Comment> comments)
        {
            foreach (var comment in comments)
            {
                if (comment.Score <= 0)
                {
                    try
                    {
                        await comment.DeleteAsync();
                    }
                    catch (RedditForbiddenException ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete comment");
                    }
                }
            }
        }

        #endregion Event Handlers
    }
}
