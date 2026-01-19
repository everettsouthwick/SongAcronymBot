using Reddit.Controllers;
using SongAcronymBot.Core.Model;
using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Core.Services.Interfaces
{
    /// <summary>
    /// Interface for matching acronyms in Reddit comments.
    /// </summary>
    public interface IAcronymMatcher
    {
        /// <summary>
        /// Finds all matching acronyms in a comment.
        /// </summary>
        /// <param name="comment">The Reddit comment to search.</param>
        /// <param name="acronyms">The list of acronyms to search for.</param>
        /// <returns>List of matched acronyms with their positions.</returns>
        List<AcronymMatch> FindMatches(Comment comment, List<EnrichedAcronym> acronyms);

        /// <summary>
        /// Checks if an acronym matches in a comment body.
        /// </summary>
        /// <param name="comment">The Reddit comment.</param>
        /// <param name="acronym">The acronym to check.</param>
        /// <param name="index">Output parameter for the match position.</param>
        /// <returns>True if the acronym matches.</returns>
        bool IsMatch(Comment comment, EnrichedAcronym acronym, out int index);
    }
}
