using SongAcronymBot.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongAcronymBot.Domain.Models
{
    public class Acronym
    {
        public int Id { get; set; }
        public string? AcronymName { get; set; }
        public AcronymType AcronymType { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        public string? TrackName { get; set; }
        public string? YearReleased { get; set; }
        public bool Enabled { get; set; }
        public Subreddit? Subreddit { get; set; }
    }
}
