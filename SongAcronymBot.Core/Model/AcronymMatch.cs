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
    }
}
