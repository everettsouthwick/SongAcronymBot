using Microsoft.Extensions.Configuration;
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using SongAcronymBot.Core.Model;
using SongAcronymBot.Repository.Models;
using SongAcronymBot.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongAcronymBot.Core.Services
{
    public interface IRedditService
    {
        Task StartAsync(RedditClient reddit);
    }

    public class RedditService : IRedditService
    {
        private readonly IAcronymRepository _acronymRepository;
        private readonly IRedditorRepository _redditorRepository;
        private readonly ISubredditRepository _subredditRepository;

        private List<Redditor> DisabledRedditors;

        private const bool DEBUG = false;

        public RedditService(IAcronymRepository acronymRepository, IRedditorRepository redditorRepository, ISubredditRepository subredditRepository)
        {
            _acronymRepository = acronymRepository;
            _redditorRepository = redditorRepository;
            _subredditRepository = subredditRepository;
        }

        public async Task StartAsync(RedditClient reddit)
        {
            DisabledRedditors = await _redditorRepository.GetAllDisabled();

            // Monitor our new unread messages for mentions
            //reddit.Account.Messages.GetMessagesUnread();
            //reddit.Account.Messages.MonitorUnread();
            //reddit.Account.Messages.UnreadUpdated += Messages_UnreadUpdated;

            // Monitor all tracked subreddits for potential matches
            var subreddits = reddit.Subreddit(await GetMultiredditStringAsync());
            subreddits.Comments.GetNew();
            subreddits.Comments.MonitorNew();
            subreddits.Comments.NewUpdated += Comments_NewUpdated;
            //subreddits.Posts.GetNew();
            //subreddits.Posts.MonitorNew();
            //subreddits.Posts.NewUpdated += Posts_NewUpdated;
        }

        private async Task ProcessMessageAsync(Reddit.Things.Message message)
        {

        }

        private async Task ProcessCommentAsync(Comment comment)
        {
            if (!IsRepliable(comment))
                return;

            if (await IsOptInOrOptOutAsync(comment))
                return;

            var matches = await FindAcronymsAsync(comment);

            if (!matches.Any())
                return;

            var replyBody = "";
            foreach(var match in matches)
            {
                replyBody += match.CommentBody;
            }
            replyBody = FormatReplyBodyWithFooter(comment, replyBody);

            if (DEBUG)
                Console.WriteLine($"DEBUG :: REPLY BODY: {replyBody}");

            await comment.ReplyAsync(replyBody);
        }

        private async Task ProcessPostAsync(Post post)
        {
            if (!IsRepliable(post))
                return;
        }

        private bool IsRepliable(Comment comment)
        {
            // Do not reply to our own submissions
            if (comment.Author.ToLower() == "songacronymbot")
            {
                if (DEBUG)
                    Console.WriteLine("DEBUG :: SKIPPING BECAUSE AUTHOR IS SELF");
                return false;
            }

            // Do not reply to submissions by someone who has disabled us
            if (DisabledRedditors.Any(x => x.Username.ToLower() == comment.Author.ToLower()))
            {
                if (DEBUG)
                    Console.WriteLine("DEBUG :: SKIPPING BECAUSE AUTHOR IS DISABLED");
                return false;
            }

            return true;
        }

        private bool IsRepliable(Post post)
        {
            // Do not reply to our own submissions
            if (post.Author.ToLower() == "songacronymbot")
            {
                if (DEBUG)
                    Console.WriteLine("DEBUG :: SKIPPING BECAUSE AUTHOR IS SELF");
                return false;
            }

            // Do not reply to submissions by someone who has disabled us
            if (DisabledRedditors.Any(x => x.Username.ToLower() == post.Author.ToLower()))
            {
                if (DEBUG)
                    Console.WriteLine("DEBUG :: SKIPPING BECAUSE AUTHOR IS DISABLED");
                return false;
            }

            return true;
        }

        private async Task<bool> IsOptInOrOptOutAsync(Comment comment)
        {
            if (comment.Root.Id.ToLower() == "j9yq8q")
                if (comment.Body.ToLower() == "optout")
                {
                    if (DEBUG)
                        Console.WriteLine("DEBUG :: USER OPTOUT");
                    await AddOrUpdateRedditor(comment.Id, comment.Author, false);
                    await comment.ReplyAsync(FormatReplyBodyWithFooter(comment, "- Your account has been disabled from receiving automatic replies.\n"));
                    DisabledRedditors = await _redditorRepository.GetAllDisabled();
                    return true;
                }
                else if (comment.Body.ToLower() == "optin")
                {
                    if (DEBUG)
                        Console.WriteLine("DEBUG :: USER OPTIN");
                    await AddOrUpdateRedditor(comment.Id, comment.Author, true);
                    await comment.ReplyAsync(FormatReplyBodyWithFooter(comment, "- Your account has been enabled for receiving automatic replies.\n"));
                    DisabledRedditors = await _redditorRepository.GetAllDisabled();
                    return true;
                }

            return false;
        }

        private async Task<List<AcronymMatch>> FindAcronymsAsync(Comment comment)
        {
            var matches = new List<AcronymMatch>();

            var acronyms = await _acronymRepository.GetAllGlobalAcronyms();
            acronyms.AddRange(await _acronymRepository.GetAllBySubredditNameAsync(comment.Subreddit.ToLower()));

            Parallel.ForEach(acronyms, acronym =>
            {
                if (IsMatch(comment, acronym, out int index))
                    matches.Add(new AcronymMatch(acronym, index));
            });

            return matches.OrderBy(x => x.Position).ToList();
        }

        private bool IsMatch(Comment comment, Acronym acronym, out int index)
        {
            var body = comment.Body.ToLower();
            var acronymName = acronym.AcronymName.ToLower();
            index = body.IndexOf(acronymName);
            if (index != -1)
            {
                try
                {
                    var matchStart = index == 0 ? 0 : index - 1;
                    var match = body.Substring(matchStart, acronymName.Length + 2);
                    match = String.Concat(Array.FindAll(match.ToCharArray(), Char.IsLetterOrDigit));

                    if (match == acronymName)
                        if (IsUnrepliedAndUndefined(comment, acronym))
                        {
                            if (DEBUG)
                                Console.WriteLine($"DEBUG :: MATCHED WORD: {match}");
                            return true;
                        }
                }
                catch (Exception ex)
                {
                    // Do nothing
                }
            }

            return false;
        }

        private bool IsUnrepliedAndUndefined(Comment comment, Acronym acronym)
        {
            var acronymName = acronym?.AcronymName?.ToLower();
            var definition = acronym?.AcronymType switch
            {
                Repository.Enum.AcronymType.Album => acronym?.AlbumName?.ToLower(),
                Repository.Enum.AcronymType.Artist => acronym?.ArtistName?.ToLower(),
                Repository.Enum.AcronymType.Single => acronym?.TrackName?.ToLower(),
                Repository.Enum.AcronymType.Track => acronym?.TrackName?.ToLower(),
                _ => acronym?.TrackName?.ToLower()
            };

            var root = comment.Root;
            var replies = root.Comments.GetComments();

            if (root.Title.ToLower().Contains(definition) || comment.Body.ToLower().Contains(definition) || comment.Subreddit.ToLower().Contains(definition))
                return false;

            foreach (var reply in replies)
            {
                if (reply.Author.ToLower() == "songacronymbot" && reply.Body.ToLower().Contains(acronymName))
                    return false;

                if (reply.NumReplies > 0)
                {
                    var subReplies = reply.replies;
                    foreach (var subReply in subReplies)
                    {
                        if (subReply.Author.ToLower() == "songacronymbot" && reply.Body.ToLower().Contains(acronymName))
                            return false;
                    }
                }
            }

            return true;
        }

        private async void Messages_UnreadUpdated(object? sender, MessagesUpdateEventArgs e)
        {
            foreach (Reddit.Things.Message message in e.Added)
            {
                if (DEBUG)
                    Console.WriteLine($"DEBUG :: New unread message {message.Author} - {message.Body}");
                await ProcessMessageAsync(message);
            }
        }

        private async void Comments_NewUpdated(object? sender, CommentsUpdateEventArgs e)
        {
            foreach (Comment comment in e.Added)
            {
                if (DEBUG)
                    Console.WriteLine($"DEBUG :: New comment {comment.Subreddit} - {comment.Root.Title}");
                await ProcessCommentAsync(comment);
            }
        }

        private async void Posts_NewUpdated(object? sender, PostsUpdateEventArgs e)
        {
            foreach (Post post in e.Added)
            {
                if (DEBUG)
                    Console.WriteLine($"DEBUG :: New post {post.Subreddit} - {post.Title}");
                await ProcessPostAsync(post);
            }
        }

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
        private async Task<List<Repository.Models.Subreddit>> GetEnabledSubredditsAsync()
        {
            return _subredditRepository.GetAll().Where(x => x.Enabled).ToList();
        }
        private async Task<Redditor> AddOrUpdateRedditor(string id, string username, bool enabled)
        {
            var redditor = await _redditorRepository.GetByIdAsync(id);

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
        private string FormatReplyBodyWithFooter(Comment comment, string body)
        {
            return $"{body}\n---\n\n^[/r/songacronymbot](/r/songacronymbot) ^(for feedback.)";
        }
        private string FormatReplyBodyWithFooter(Post post, string body)
        {
            return $"{body}\n---\n\n^[/u/{post.Author}](/u/{post.Author}) ^(can reply with \"delete\" to remove comment. |) ^[/r/songacronymbot](/r/songacronymbot) ^(for feedback.)";
        }
    }
}
