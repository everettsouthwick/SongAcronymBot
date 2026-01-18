namespace SongAcronymBot.Domain.Supabase.Models
{
    /// <summary>
    /// DTO that combines acronym data with related artist/album/track info
    /// for use by AcronymMatch when formatting comment replies.
    /// </summary>
    public class EnrichedAcronym
    {
        public Guid Id { get; set; }
        public string AcronymText { get; set; } = string.Empty;
        public AcronymType AcronymType { get; set; }
        public bool IsActive { get; set; }

        // Artist info (always present)
        public Guid? ArtistId { get; set; }
        public string? ArtistName { get; set; }
        public string? ArtistSlug { get; set; }

        // Album info (present for Album and Track types)
        public Guid? AlbumId { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumSlug { get; set; }
        public int? YearReleased { get; set; }

        // Track info (present for Track and Single types)
        public Guid? TrackId { get; set; }
        public string? TrackName { get; set; }
        public bool IsSingle { get; set; }
    }
}
