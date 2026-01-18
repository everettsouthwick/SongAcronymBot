using SongAcronymBot.Domain.Supabase.Models;
using System.Text.RegularExpressions;

namespace SongAcronymBot.Core.Model
{
    public partial class AcronymMatch
    {
        public string? Acronym { get; set; }
        public string? CommentBody { get; set; }
        public int Position { get; set; }

        public AcronymMatch(EnrichedAcronym acronym, int index)
        {
            Acronym = acronym.AcronymText;

            var artistLink = !string.IsNullOrEmpty(acronym.ArtistSlug)
                ? $"[{acronym.ArtistName}](https://www.myartistradar.com/artists/{acronym.ArtistSlug})"
                : acronym.ArtistName;

            var albumLink = !string.IsNullOrEmpty(acronym.AlbumSlug) && !string.IsNullOrEmpty(acronym.ArtistSlug)
                ? $"[{acronym.AlbumName}](https://www.myartistradar.com/artists/{acronym.ArtistSlug}/{acronym.AlbumSlug})"
                : acronym.AlbumName;

            string? commentBody = null;

            if (acronym.AcronymType == AcronymType.Track && IsTrackAndAlbumNameSimilar(acronym.TrackName, acronym.AlbumName))
            {
                commentBody = $"- {acronym.AcronymText} could mean \"{acronym.TrackName}\" (track) or *{albumLink}* (album) ({acronym.YearReleased}) by {artistLink}.\n";
            }

            if (commentBody == null)
            {
                commentBody = acronym.AcronymType switch
                {
                    AcronymType.Album => $"- {acronym.AcronymText} could mean *{albumLink}* ({acronym.YearReleased}), an album by {artistLink}.\n",
                    AcronymType.Artist => $"- {acronym.AcronymText} could mean {artistLink}.\n",
                    AcronymType.Single => $"- {acronym.AcronymText} could mean \"{acronym.TrackName}\", a single by {artistLink}.\n",
                    AcronymType.Track => $"- {acronym.AcronymText} could mean \"{acronym.TrackName}\", a track from *{albumLink}* ({acronym.YearReleased}) by {artistLink}.\n",
                    _ => $"- {acronym.AcronymText} could mean {acronym.TrackName}, a track from *{albumLink}* ({acronym.YearReleased}) by {artistLink}.\n",
                };
            }
            CommentBody = commentBody;
            Position = index;
        }

        public AcronymMatch(string acronymName, int index)
        {
            Acronym = acronymName;
            CommentBody = $"- {acronymName} was not recognized. [Click here](https://www.reddit.com/r/songacronymbot/comments/qxsnga/new_acronym_suggestions/) to suggest this to be added.\n";
            Position = index;
        }

        private static bool IsTrackAndAlbumNameSimilar(string? trackName, string? albumName)
        {
            if (string.IsNullOrEmpty(trackName) || string.IsNullOrEmpty(albumName))
                return false;

            // Remove common suffixes/prefixes that might differ between track and album names
            var normalizedTrack = NormalizeName(trackName);
            var normalizedAlbum = NormalizeName(albumName);

            // Check if they're exactly the same after normalization
            if (normalizedTrack.Equals(normalizedAlbum, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if one contains the other (for cases like "Song Name" vs "Song Name (feat. Artist)")
            return normalizedTrack.Contains(normalizedAlbum, StringComparison.OrdinalIgnoreCase) ||
                   normalizedAlbum.Contains(normalizedTrack, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var normalized = name.Trim();

            // Remove featured artist information: (feat. Artist), (featuring Artist), (ft. Artist), etc.
            normalized = FeatRegex().Replace(normalized, "");

            // Remove other common track-specific suffixes like (remix), (version), (edit) - broadly matching parens with these words
            normalized = VersionRegex().Replace(normalized, "");

            // Remove extra whitespace
            normalized = WhitespaceRegex().Replace(normalized, " ").Trim();

            return normalized;
        }

        [GeneratedRegex(@"\s*\((feat|ft|featuring)\.?\s+[^)]+\)", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex FeatRegex();

        [GeneratedRegex(@"\s*\([^)]*(remix|version|edit)[^)]*\)", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex VersionRegex();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();
    }
}
