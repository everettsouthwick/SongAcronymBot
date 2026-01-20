using Microsoft.Extensions.Logging;
using Reddit.Controllers;
using SongAcronymBot.Core.Model;
using SongAcronymBot.Core.Services.Interfaces;
using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Core.Services
{
    /// <summary>
    /// Handles matching acronyms in Reddit comment text.
    /// </summary>
    public class AcronymMatcher(ILogger<AcronymMatcher> logger) : IAcronymMatcher
    {
        private readonly ILogger<AcronymMatcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <inheritdoc/>
        public List<AcronymMatch> FindMatches(Comment comment, List<EnrichedAcronym> acronyms)
        {
            _logger.LogTrace("Checking {Count} acronyms in r/{Subreddit}", acronyms.Count, comment.Subreddit);
            var matches = new List<AcronymMatch>();

            foreach (var acronym in acronyms)
            {
                if (IsMatch(comment, acronym, out int index))
                {
                    matches.Add(new AcronymMatch(acronym, index));
                }
            }

            return [.. matches.OrderBy(x => x.Position)];
        }

        /// <inheritdoc/>
        public bool IsMatch(Comment comment, EnrichedAcronym acronym, out int index)
        {
            index = -1;

            if (acronym?.AcronymText == null)
            {
                return false;
            }

            var body = comment.Body.ToLower();
            var acronymName = acronym.AcronymText.ToLower();

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
                            _logger.LogDebug("Acronym match found: {Acronym}", match);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Acronym matching error in r/{Subreddit} for '{Acronym}'", comment.Subreddit, acronym.AcronymText);
                }
            }

            return false;
        }

        private static bool IsUnrepliedAndUndefined(Comment comment, EnrichedAcronym acronym)
        {
            if (acronym?.AcronymText == null)
            {
                return true;
            }

            var acronymName = acronym.AcronymText.ToLower();
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
    }
}
