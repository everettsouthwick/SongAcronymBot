using Microsoft.EntityFrameworkCore;
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using Reddit.Exceptions;
using SongAcronymBot.Core.Model;
using SongAcronymBot.Domain.Enum;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

namespace SongAcronymBot.Core.Services
{
    public interface IRedditService
    {
        Task StartAsync(RedditClient reddit, bool debug = false);
    }

    public class RedditService : IRedditService
    {
        private readonly IAcronymRepository _acronymRepository;
        private readonly IRedditorRepository _redditorRepository;
        private readonly ISubredditRepository _subredditRepository;
        private readonly ISpotifyService _spotifyService;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private RedditClient Reddit = null!;
        private List<Redditor> DisabledRedditors = null!;
        private bool Debug;

        public RedditService(IAcronymRepository acronymRepository, IRedditorRepository redditorRepository, ISubredditRepository subredditRepository, ISpotifyService spotifyService)
        {
            _acronymRepository = acronymRepository ?? throw new ArgumentNullException(nameof(acronymRepository));
            _redditorRepository = redditorRepository ?? throw new ArgumentNullException(nameof(redditorRepository));
            _subredditRepository = subredditRepository ?? throw new ArgumentNullException(nameof(subredditRepository));
            _spotifyService = spotifyService ?? throw new ArgumentNullException(nameof(spotifyService));
        }

        public async Task StartAsync(RedditClient reddit, bool debug = false)
        {
            ArgumentNullException.ThrowIfNull(reddit);

            Reddit = reddit;
            DisabledRedditors = await _redditorRepository.GetAllDisabled();
            Debug = debug;

            // Monitor our new unread messages for mentions
            reddit.Account.Messages.GetMessagesUnread();
            reddit.Account.Messages.MonitorUnread();
            reddit.Account.Messages.UnreadUpdated += Messages_UnreadUpdated;
            reddit.Account.Me.GetCommentHistory();
            reddit.Account.Me.MonitorCommentHistory();
            reddit.Account.Me.CommentHistoryUpdated += Me_CommentHistoryUpdated;

            // Monitor all tracked subreddits for potential matches
            var subreddits = reddit.Subreddit(await GetMultiredditStringAsync());
            subreddits.Comments.GetNew();
            subreddits.Comments.MonitorNew();
            subreddits.Comments.NewUpdated += Comments_NewUpdated;
        }

        #region Process Message

        private async void Messages_UnreadUpdated(object? sender, MessagesUpdateEventArgs e)
        {
            foreach (Reddit.Things.Message message in e.Added)
            {
                if (Debug)
                {
                    Console.WriteLine($"DEBUG :: New unread message {message.Author} - {message.Body}");
                }
                await ProcessMessageAsync(message);
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

            if (Debug)
            {
                Console.WriteLine($"DEBUG :: REPLY BODY: {replyBody}");
            }

            try
            {
                var comment = Reddit.Comment($"t1_{message.Id}").About();
                await comment.ReplyAsync(replyBody);
            }
            catch (RedditForbiddenException ex)
            {
                if (Debug)
                {
                    Console.WriteLine($"DEBUG :: Failed to reply - {ex.Message}");
                }
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

                await AddOrUpdateRedditor(message.Id, message.Author, false);
                DisabledRedditors = await _redditorRepository.GetAllDisabled();

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
                await AddOrUpdateRedditor(message.Id, message.Author, false);
                DisabledRedditors = await _redditorRepository.GetAllDisabled();
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
            var index = 1;
            foreach (var query in acronymsToQuery)
            {
                var acronyms = (await _acronymRepository.GetAllByNameAsync(query)).GroupBy(x => x.ArtistName).Select(x => x.First()).ToList();
                foreach (var acronym in acronyms)
                {
                    matches.Add(new AcronymMatch(acronym, index));
                }

                if (acronyms.Count == 0)
                {
                    var acronym = await _spotifyService.SearchAcronymAsync(query);
                    if (acronym != null)
                    {
                        matches.Add(new AcronymMatch(acronym, index));
                    }
                    else
                    {
                        matches.Add(new AcronymMatch(query, index));
                    }
                }

                index++;
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
            foreach (Comment comment in e.Added)
            {
                if (Debug)
                {
                    Console.WriteLine($"DEBUG :: New comment {comment.Subreddit} - {comment.Root.Title}");
                }
                await ProcessCommentAsync(comment);
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

            if (Debug)
            {
                Console.WriteLine($"DEBUG :: REPLY BODY: {replyBody}");
            }

            try
            {
                await comment.ReplyAsync(replyBody);
            }
            catch (RedditForbiddenException ex)
            {
                if (Debug)
                {
                    Console.WriteLine($"DEBUG :: Failed to reply - {ex.Message}");
                }
            }
        }

        private bool IsRepliable(Comment comment)
        {
            // Do not reply to our own submissions
            if (comment.Author.Equals("songacronymbot", StringComparison.CurrentCultureIgnoreCase))
            {
                if (Debug)
                {
                    Console.WriteLine("DEBUG :: SKIPPING BECAUSE AUTHOR IS SELF");
                }
                return false;
            }

            // Do not reply to submissions by someone who has disabled us
            if (DisabledRedditors.Any(x => (x.Username ?? string.Empty).Equals(comment.Author, StringComparison.CurrentCultureIgnoreCase)))
            {
                if (Debug)
                {
                    Console.WriteLine("DEBUG :: SKIPPING BECAUSE AUTHOR IS DISABLED");
                }
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
                    if (Debug)
                    {
                        Console.WriteLine("DEBUG :: USER OPTOUT");
                    }
                    await AddOrUpdateRedditor(comment.Id, comment.Author, false);
                    try
                    {
                        await comment.ReplyAsync(FormatReplyBodyWithFooter("- Your account has been disabled from receiving automatic replies.\n", comment.Author));
                    }
                    catch (RedditForbiddenException ex)
                    {
                        if (Debug)
                        {
                            Console.WriteLine($"DEBUG :: Failed to reply - {ex.Message}");
                        }
                    }
                    DisabledRedditors = await _redditorRepository.GetAllDisabled();
                    return true;
                }
                else if (comment.Body.Equals("optin", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Debug)
                    {
                        Console.WriteLine("DEBUG :: USER OPTIN");
                    }
                    await AddOrUpdateRedditor(comment.Id, comment.Author, true);
                    try
                    {
                        await comment.ReplyAsync(FormatReplyBodyWithFooter("- Your account has been enabled for receiving automatic replies.\n", comment.Author));
                    }
                    catch (RedditForbiddenException ex)
                    {
                        if (Debug)
                        {
                            Console.WriteLine($"DEBUG :: Failed to reply - {ex.Message}");
                        }
                    }
                    DisabledRedditors = await _redditorRepository.GetAllDisabled();
                    return true;
                }
            }

            return false;
        }

        public async Task<List<AcronymMatch>> FindAcronymsAsync(Comment comment)
        {
            var matches = new List<AcronymMatch>();

            await _semaphore.WaitAsync();
            try
            {
                var acronyms = await _acronymRepository.GetAllGlobalAcronyms();
                acronyms.AddRange(await _acronymRepository.GetAllBySubredditNameAsync(comment.Subreddit.ToLower()));

                foreach (var acronym in acronyms)
                {
                    if (IsMatch(comment, acronym, out int index))
                    {
                        matches.Add(new AcronymMatch(acronym, index));
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return matches.OrderBy(x => x.Position).ToList();
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
                            if (Debug)
                            {
                                Console.WriteLine($"DEBUG :: MATCHED WORD: {match}");
                            }
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
            if (Debug)
            {
                Console.WriteLine($"DEBUG :: New comment history activity.");
            }

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
                        if (Debug)
                        {
                            Console.WriteLine($"DEBUG :: Failed to delete comment - {ex.Message}");
                        }
                    }
                }
            }
        }

        #endregion Process Comment Updates

        #region Shared Functionality

        private async Task<string> GetMultiredditStringAsync()
        {
            var multireddit = string.Empty;

            var subreddits = await GetEnabledSubredditsAsync();
            foreach (var subreddit in subreddits)
            {
                multireddit += $"{subreddit.Name}+";
            }

            return multireddit.TrimEnd('+');
        }

        private async Task<List<Domain.Models.Subreddit>> GetEnabledSubredditsAsync()
        {
            return await _subredditRepository.GetAll().Where(x => x.Enabled).ToListAsync();
        }

        private async Task<Redditor> AddOrUpdateRedditor(string id, string username, bool enabled)
        {
            var redditor = await _redditorRepository.GetByNameAsync(username);

            if (redditor == null)
            {
                redditor = new Redditor
                {
                    Id = id,
                    Username = username,
                    Enabled = enabled
                };
                await _redditorRepository.AddAsync(redditor);
            }
            else
            {
                redditor.Enabled = enabled;
                await _redditorRepository.UpdateAsync(redditor);
            }

            return redditor;
        }

        private static string FormatReplyBodyWithFooter(string body, string author)
        {
            var random = new Random();
            var showSeren = random.NextDouble() <= 0.01;

            var serenTexts = new[]
            {
                "Powered by Seren AI",
                "Powered with Seren AI",
                "Guided by Seren AI",
                "Guided with Seren AI",
                "Using Seren AI",
                "Featuring Seren AI",
                "Made better by Seren AI",
                "Made better with Seren AI",
                "Augmented by Seren AI",
                "Augmented with Seren AI",
                "Elevated by Seren AI",
                "Elevated with Seren AI",
                "Optimized by Seren AI",
                "Optimized with Seren AI",
                "Improved by Seren AI",
                "Improved with Seren AI",
                "Enhanced by Seren AI",
                "Enhanced with Seren AI",
            };

            var serenText = serenTexts[random.Next(serenTexts.Length)];

            var footer = showSeren 
                ? $"^([{serenText}](https://www.getseren.com/) | [/u/{author}](/u/{author}) ^(can reply with \"delete\" to remove comment. |) ^[/r/songacronymbot](/r/songacronymbot) ^(for feedback.)"
                : $"^[/u/{author}](/u/{author}) ^(can reply with \"delete\" to remove comment. |) ^[/r/songacronymbot](/r/songacronymbot) ^(for feedback.)";

            return $"{body}\n---\n\n{footer}";
        }

        #endregion Shared Functionality
    }
}