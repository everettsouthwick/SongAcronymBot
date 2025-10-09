using SongAcronymBot.Domain.Enum;
using SongAcronymBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongAcronymBot.Core.Model
{
    public class AcronymMatch
    {
        public string? Acronym { get; set; }
        public string? CommentBody { get; set; }
        public int Position { get; set; }

        public AcronymMatch(Acronym acronym, int index)
        {
            Acronym = acronym.AcronymName;
            CommentBody = acronym.AcronymType switch
            {
                AcronymType.Album => $"- {acronym.AcronymName} could mean *{acronym.AlbumName}* ({acronym.YearReleased}), an album by {acronym.ArtistName}.\n",
                AcronymType.Artist => $"- {acronym.AcronymName} could mean {acronym.ArtistName}.\n",
                AcronymType.Single => $"- {acronym.AcronymName} could mean \"{acronym.TrackName}\", a single by {acronym.ArtistName}.\n",
                AcronymType.Track => IsTrackAndAlbumNameSimilar(acronym.TrackName, acronym.AlbumName)
                    ? $"- {acronym.AcronymName} could mean \"{acronym.TrackName}\" (track) or *{acronym.AlbumName}* (album) ({acronym.YearReleased}) by {acronym.ArtistName}.\n"
                    : $"- {acronym.AcronymName} could mean \"{acronym.TrackName}\", a track from *{acronym.AlbumName}* ({acronym.YearReleased}) by {acronym.ArtistName}.\n",
                _ => $"- {acronym.AcronymName} could mean {acronym.TrackName}, a track from *{acronym.AlbumName}* ({acronym.YearReleased}) by {acronym.ArtistName}.\n",
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
