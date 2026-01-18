using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SongAcronymBot.Domain.Supabase.Models
{
    [Table("tracks")]
    public class Track : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("artist_id")]
        public Guid ArtistId { get; set; }

        [Column("album_id")]
        public Guid? AlbumId { get; set; } // Nullable for singles

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("slug")]
        public string Slug { get; set; } = string.Empty;

        [Column("spotify_track_id")]
        public string? SpotifyTrackId { get; set; }

        [Column("spotify_url")]
        public string? SpotifyUrl { get; set; }

        [Column("track_number")]
        public int? TrackNumber { get; set; }

        [Column("is_single")]
        public bool IsSingle { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
