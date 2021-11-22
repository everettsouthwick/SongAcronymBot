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
                AcronymType.Track => $"- {acronym.AcronymName} could mean \"{acronym.TrackName}\", a track from *{acronym.AlbumName}* ({acronym.YearReleased}) by {acronym.ArtistName}.\n",
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
    }
}
