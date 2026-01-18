using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Core.Model
{
    public class AcronymMatch
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

            CommentBody = acronym.AcronymType switch
            {
                AcronymType.Album => $"- {acronym.AcronymText} could mean *{albumLink}* ({acronym.YearReleased}), an album by {artistLink}.\n",
                AcronymType.Artist => $"- {acronym.AcronymText} could mean {artistLink}.\n",
                AcronymType.Single => $"- {acronym.AcronymText} could mean \"{acronym.TrackName}\", a single by {artistLink}.\n",
                AcronymType.Track => $"- {acronym.AcronymText} could mean \"{acronym.TrackName}\", a track from *{albumLink}* ({acronym.YearReleased}) by {artistLink}.\n",
                _ => $"- {acronym.AcronymText} could mean {acronym.TrackName}, a track from *{albumLink}* ({acronym.YearReleased}) by {artistLink}.\n",
            };
            Position = index;
        }

        public AcronymMatch(string acronymName, int index)
        {
            Acronym = acronymName;
            CommentBody = $"- {acronymName} was not recognized. [Click here](https://www.reddit.com/r/songacronymbot/comments/qxsnga/new_acronym_suggestions/) to suggest this to be added.\n";
            Position = index;
        }

        private static bool IsTrackAndAlbumNameSimilar(string trackName, string albumName)
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

            // Remove common patterns that might appear in track names but not album names
            var normalized = name.Trim();

            // Remove featured artist information: (feat. Artist), (featuring Artist), (ft. Artist), etc.
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*\(feat\.?\s+[^)]+\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*\(featuring\s+[^)]+\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*\(ft\.?\s+[^)]+\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove other common track-specific suffixes
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*\([^)]*remix[^)]*\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*\([^)]*version[^)]*\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*\([^)]*edit[^)]*\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove extra whitespace
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }
    }
}
