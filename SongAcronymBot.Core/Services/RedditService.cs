using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using Reddit.Exceptions;
using SongAcronymBot.Core.Model;
using SongAcronymBot.Domain.Enum;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Repositories;
using IOptedOutRedditorRepository = SongAcronymBot.Domain.Supabase.Repositories.IOptedOutRedditorRepository;
using OptedOutRedditorModel = SongAcronymBot.Domain.Supabase.Models.OptedOutRedditor;

namespace SongAcronymBot.Core.Services
{
    public interface IRedditService
    {
        Task StartAsync(RedditClient reddit);
    }

    public class RedditService(
        IAcronymRepository acronymRepository,
        IOptedOutRedditorRepository optedOutRedditorRepository,
        ILogger<RedditService> logger) : IRedditService
    {
        private readonly IAcronymRepository _acronymRepository = acronymRepository ?? throw new ArgumentNullException(nameof(acronymRepository));
        private readonly IOptedOutRedditorRepository _optedOutRedditorRepository = optedOutRedditorRepository ?? throw new ArgumentNullException(nameof(optedOutRedditorRepository));
        private readonly ILogger<RedditService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private RedditClient Reddit = null!;
        private volatile HashSet<string> DisabledRedditors = null!; // Made volatile for thread safety, HashSet for O(1) lookups

        // Cache for subreddit acronyms
        private readonly Dictionary<string, List<Acronym>> SubredditAcronymsCache = [];

        private readonly Dictionary<string, DateTime> LastSubredditAcronymsUpdate = [];
        private readonly TimeSpan SubredditAcronymsCacheTimeout = TimeSpan.FromHours(6);

        private System.Timers.Timer _commentCheckTimer = null!;

        public async Task StartAsync(RedditClient reddit)
        {
            ArgumentNullException.ThrowIfNull(reddit);

            Reddit = reddit;

            DisabledRedditors = await _optedOutRedditorRepository.GetAllUsernamesAsync();
            _logger.LogDebug("Retrieved {Count} opted-out redditors", DisabledRedditors.Count);

            try
            {
                // Monitor our new unread messages for mentions
                reddit.Account.Messages.GetMessagesUnread();
                reddit.Account.Messages.MonitorUnread();
                reddit.Account.Messages.UnreadUpdated += Messages_UnreadUpdated;
                reddit.Account.Me.GetCommentHistory();
                reddit.Account.Me.MonitorCommentHistory();
                reddit.Account.Me.CommentHistoryUpdated += Me_CommentHistoryUpdated;
                // Set up timer to check comments every 10 minutes
                await CheckRecentComments();
                _commentCheckTimer = new System.Timers.Timer(TimeSpan.FromMinutes(10).TotalMilliseconds);
                _commentCheckTimer.Elapsed += async (s, e) => await CheckRecentComments();
                _commentCheckTimer.Start();

                // Monitor all tracked subreddits for potential matches
                var subredditString = string.Join("+", reddit.Account.Me.Multis()
                    .Where(x => x.Name.StartsWith("tracked"))
                    .SelectMany(x => x.Subreddits)
                    .Select(s => s.Name) ?? []);
                var trackedSubreddits = reddit.Subreddit(subredditString);

                _logger.LogInformation("Monitoring subreddits: {Subreddits}", subredditString);

                trackedSubreddits.Comments.MonitorNew();
                trackedSubreddits.Comments.NewUpdated += Comments_NewUpdated;
            }
            catch (Exception ex) when (ex is RedditForbiddenException or RedditBadGatewayException)
            {
                _logger.LogError(ex, "Failed to start Reddit service");
                throw;
            }
        }

        private async Task CheckRecentComments()
        {
            try
            {
                var comments = Reddit.Account.Me.GetCommentHistory(limit: 50);
                var recentComments = comments.Where(c => (DateTimeOffset.UtcNow - c.Created).TotalDays <= 1);

                foreach (var comment in recentComments)
                {
                    if (comment.Score >= 999 && !comment.Body.Contains("Seren"))
                    {
                        try
                        {
                            var random = new Random();
                            var serenTexts = new[]
                            {
                                "^Powered ^by ^Seren ^AI ",
                                "^Guided ^by ^Seren ^AI ",
                            };

                            var serenText = serenTexts[random.Next(serenTexts.Length)];
                            var newBody = $"{comment.Body}\n\n[{serenText}](https://www.getseren.com/?utm_source=reddit&utm_medium=comment&utm_campaign=songacronymbot)";
                            await comment.EditAsync(newBody);

                            _logger.LogDebug("Added Seren disclaimer to comment {CommentId}", comment.Id);
                        }
                        catch (RedditForbiddenException ex)
                        {
                            _logger.LogWarning(ex, "Failed to edit comment");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check recent comments");
            }
        }

        #region Process Message

        private async void Messages_UnreadUpdated(object? sender, MessagesUpdateEventArgs e)
        {
            foreach (var message in e.Added)
            {
                _logger.LogDebug("New unread message from {Author}: {Body}", message.Author, message.Body);
                try
                {
                    await ProcessMessageAsync(message);
                }
                catch (RedditForbiddenException ex)
                {
                    _logger.LogWarning(ex, "Failed to process message");
                }
            }
        }

        private async Task ProcessMessageAsync(Reddit.Things.Message message)
        {
            if (await IsBadBotAsync(message))
            {
                return;
            }

            if (await IsDeleteAsync(message))
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
                return;
            }

            var replyBody = "";
            foreach (var match in matches)
            {
                replyBody += match.CommentBody;
            }
            replyBody = FormatReplyBodyWithFooter(replyBody, message.Author);

            _logger.LogDebug("Reply body: {ReplyBody}", replyBody);

            try
            {
                var comment = Reddit.Comment($"t1_{message.Id}").About();
                await comment.ReplyAsync(replyBody);
            }
            catch (RedditForbiddenException ex)
            {
                _logger.LogWarning(ex, "Failed to reply");
            }
        }

        private async Task<bool> IsBadBotAsync(Reddit.Things.Message message)
        {
            if (message.Subject != "comment reply" || !message.Body.Equals("bad bot", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            var parent = Reddit.Comment(message.ParentId).About();

            if (parent.Author.Equals("songacronymbot", StringComparison.CurrentCultureIgnoreCase))
            {
                if (parent.UpVotes < 5)
                {
                    await parent.DeleteAsync();
                }

                await AddOptedOutRedditorAsync(message.Author);
                _logger.LogDebug("Refreshing opted-out redditors list after bad bot response...");
                DisabledRedditors = await _optedOutRedditorRepository.GetAllUsernamesAsync();
                _logger.LogDebug("Retrieved {Count} opted-out redditors", DisabledRedditors.Count);

                return true;
            }

            return false;
        }

        private async Task<bool> IsDeleteAsync(Reddit.Things.Message message)
        {
            if (message.Subject != "comment reply" || !message.Body.Equals("delete", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            var parent = Reddit.Comment(message.ParentId).About();

            if (!parent.Author.Equals("songacronymbot", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if (parent.Body.Contains(message.Author, StringComparison.CurrentCultureIgnoreCase))
            {
                await parent.DeleteAsync();
                await AddOptedOutRedditorAsync(message.Author);
                _logger.LogDebug("Refreshing opted-out redditors list after delete request...");
                DisabledRedditors = await _optedOutRedditorRepository.GetAllUsernamesAsync();
                _logger.LogDebug("Retrieved {Count} opted-out redditors", DisabledRedditors.Count);
                return true;
            }

            return false;
        }

        private static bool IsNotSummon(Reddit.Things.Message message)
        {
            if (message.Subject == "username mention" && message.WasComment)
            {
                return false;
            }

            return true;
        }

        private async Task<List<AcronymMatch>> FindAcronymsAsync(Reddit.Things.Message message)
        {
            var matches = new List<AcronymMatch>();

            var acronymsToQuery = ParseAcronymsFromMention(message);

            for (int i = 0; i < acronymsToQuery.Count; i++)
            {
                var query = acronymsToQuery[i];
                var acronyms = (await _acronymRepository.GetAllByNameAsync(query)).GroupBy(x => x.ArtistName).Select(x => x.First()).ToList();

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

        private static List<string> ParseAcronymsFromMention(Reddit.Things.Message message)
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

        #endregion Process Message

        #region Process Comment

        private async void Comments_NewUpdated(object? sender, CommentsUpdateEventArgs e)
        {
            foreach (var comment in e.Added)
            {
                _logger.LogDebug("New comment in {Subreddit}: {Title}", comment.Subreddit, comment.Root.Title);
                try
                {
                    await ProcessCommentAsync(comment);
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

        private async Task ProcessCommentAsync(Comment comment)
        {
            if (!IsRepliable(comment))
            {
                return;
            }

            if (await IsOptInOrOptOutAsync(comment))
            {
                return;
            }

            var matches = await FindAcronymsAsync(comment);

            if (matches.Count == 0)
            {
                return;
            }

            var replyBody = "";
            foreach (var match in matches)
            {
                replyBody += match.CommentBody;
            }
            replyBody = FormatReplyBodyWithFooter(replyBody, comment.Author);

            _logger.LogDebug("Reply body: {ReplyBody}", replyBody);

            try
            {
                await comment.ReplyAsync(replyBody);
            }
            catch (RedditForbiddenException ex)
            {
                _logger.LogWarning(ex, "Failed to reply");
                throw;
            }
        }

        private bool IsRepliable(Comment comment)
        {
            // Do not reply to our own submissions
            if (comment.Author.Equals("songacronymbot", StringComparison.CurrentCultureIgnoreCase))
            {
                _logger.LogTrace("Skipping comment from self");
                return false;
            }

            // Do not reply to submissions by someone who has disabled us
            if (DisabledRedditors.Contains(comment.Author))
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

        private async Task<bool> IsOptInOrOptOutAsync(Comment comment)
        {
            if (comment.Root.Id.Equals("j9yq8q", StringComparison.CurrentCultureIgnoreCase))
            {
                if (comment.Body.Equals("optout", StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogInformation("User {Author} opted out", comment.Author);
                    await AddOptedOutRedditorAsync(comment.Author);
                    try
                    {
                        await comment.ReplyAsync(FormatReplyBodyWithFooter("- Your account has been disabled from receiving automatic replies.\n", comment.Author));
                    }
                    catch (RedditForbiddenException ex)
                    {
                        _logger.LogWarning(ex, "Failed to reply to opt-out");
                    }
                    _logger.LogDebug("Refreshing opted-out redditors list after user optout...");
                    DisabledRedditors = await _optedOutRedditorRepository.GetAllUsernamesAsync();
                    _logger.LogDebug("Retrieved {Count} opted-out redditors", DisabledRedditors.Count);
                    return true;
                }
                else if (comment.Body.Equals("optin", StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogInformation("User {Author} opted in", comment.Author);
                    await RemoveOptedOutRedditorAsync(comment.Author);
                    try
                    {
                        await comment.ReplyAsync(FormatReplyBodyWithFooter("- Your account has been enabled for receiving automatic replies.\n", comment.Author));
                    }
                    catch (RedditForbiddenException ex)
                    {
                        _logger.LogWarning(ex, "Failed to reply to opt-in");
                    }
                    _logger.LogDebug("Refreshing opted-out redditors list after user optin...");
                    DisabledRedditors = await _optedOutRedditorRepository.GetAllUsernamesAsync();
                    _logger.LogDebug("Retrieved {Count} opted-out redditors", DisabledRedditors.Count);
                    return true;
                }
            }

            return false;
        }

        public async Task<List<AcronymMatch>> FindAcronymsAsync(Comment comment)
        {
            var matches = new List<AcronymMatch>();

            var acronyms = new List<Acronym>();

            // Check if subreddit acronyms cache needs refresh
            var subredditName = comment.Subreddit.ToLower();
            if (!SubredditAcronymsCache.TryGetValue(subredditName, out List<Acronym>? value) ||
                DateTime.UtcNow - LastSubredditAcronymsUpdate[subredditName] > SubredditAcronymsCacheTimeout)
            {
                _logger.LogDebug("Refreshing subreddit acronyms cache for {Subreddit}", subredditName);

                try
                {
                    value = await _acronymRepository.GetAllBySubredditNameAsync(subredditName);
                    SubredditAcronymsCache[subredditName] = value;
                    LastSubredditAcronymsUpdate[subredditName] = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh subreddit acronyms cache for {Subreddit}", subredditName);
                    value = new List<Acronym>();
                }
            }

            acronyms.AddRange(value);

            foreach (var acronym in acronyms)
            {
                if (IsMatch(comment, acronym, out int index))
                {
                    matches.Add(new AcronymMatch(acronym, index));
                }
            }

            return [.. matches.OrderBy(x => x.Position)];
        }

        private bool IsMatch(Comment comment, Acronym acronym, out int index)
        {
            index = -1;

            if (acronym?.AcronymName == null)
            {
                return false;
            }

            var body = comment.Body.ToLower();
            var acronymName = acronym.AcronymName.ToLower();

            index = body.IndexOf(acronymName);
            if (index != -1)
            {
                try
                {
                    var matchStart = index == 0 ? 0 : index - 1;
                    var matchLength = acronymName.Length + 2 > body.Length ? acronymName.Length : acronymName.Length + 2;
                    var match = body.Substring(matchStart, matchLength);
                    match = string.Concat(Array.FindAll(match.ToCharArray(), char.IsLetterOrDigit));
                    acronymName = string.Concat(Array.FindAll(acronymName.ToCharArray(), char.IsLetterOrDigit));

                    if (match == acronymName)
                    {
                        if (IsUnrepliedAndUndefined(comment, acronym))
                        {
                            _logger.LogDebug("Matched word: {Match}", match);
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    // Do nothing
                }
            }

            return false;
        }

        private static bool IsUnrepliedAndUndefined(Comment comment, Acronym acronym)
        {
            if (acronym?.AcronymName == null)
            {
                return true;
            }

            var acronymName = acronym.AcronymName.ToLower();
            var definition = acronym.AcronymType switch
            {
                AcronymType.Album => acronym.AlbumName?.ToLower(),
                AcronymType.Artist => acronym.ArtistName?.ToLower(),
                AcronymType.Single => acronym.TrackName?.ToLower(),
                AcronymType.Track => acronym.TrackName?.ToLower(),
                _ => acronym.TrackName?.ToLower()
            };

            if (definition == null)
            {
                return true;
            }

            var root = comment.Root;
            var replies = GetCommentTree(root.Comments.GetComments(limit: 500));

            if (root.Title.Contains(definition, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            foreach (var reply in replies)
            {
                var body = reply.Body.ToLower();
                if ((reply.Author.Equals("songacronymbot", StringComparison.CurrentCultureIgnoreCase) && body.Contains(acronymName)) || body.Contains(definition))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<Comment> GetCommentTree(List<Comment> comments)
        {
            var commentTree = new List<Comment>();
            commentTree.AddRange(comments);

            foreach (var comment in comments)
            {
                GetCommentTree(comment.Replies);
                commentTree.AddRange(GetCommentTree(GetMoreChildren(comment, commentTree)));
            }

            return commentTree;
        }

        private static List<Comment> GetMoreChildren(Comment comment, List<Comment> commentTree)
        {
            List<Comment> children = [];

            if (comment.NumReplies == 0)
            {
                return children;
            }

            foreach (var child in comment.Replies)
            {
                if (!commentTree.Any(x => x.Id == child.Id))
                {
                    children.Add(child);
                }
            }

            return children;
        }

        #endregion Process Comment

        #region Process Comment Updates

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

        #endregion Process Comment Updates

        #region Shared Functionality

        private async Task AddOptedOutRedditorAsync(string username)
        {
            var existingRedditor = await _optedOutRedditorRepository.GetByUsernameAsync(username);
            if (existingRedditor != null)
            {
                return; // Already opted out
            }

            var optedOutRedditor = new OptedOutRedditorModel
            {
                Id = Guid.NewGuid(),
                Username = username,
                OptedOutAt = DateTime.UtcNow
            };
            await _optedOutRedditorRepository.CreateAsync(optedOutRedditor);
        }

        private async Task RemoveOptedOutRedditorAsync(string username)
        {
            var existingRedditor = await _optedOutRedditorRepository.GetByUsernameAsync(username);
            if (existingRedditor == null)
            {
                return; // Not opted out
            }

            await _optedOutRedditorRepository.DeleteAsync(existingRedditor.Id);
        }

        private static string FormatReplyBodyWithFooter(string body, string author)
        {
            return $"{body}\n---\n\n^[/u/{author}](/u/{author}) ^(can reply with \"delete\" to remove comment. |) ^[/r/songacronymbot](/r/songacronymbot) ^(for feedback.)";
        }
    }

    #endregion Shared Functionality
}
