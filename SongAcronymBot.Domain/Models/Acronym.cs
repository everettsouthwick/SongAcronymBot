using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SongAcronymBot.Domain.Supabase.Models
{
    [Table("acronyms")]
    public class Acronym : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("artist_id")]
        public Guid? ArtistId { get; set; }

        [Column("album_id")]
        public Guid? AlbumId { get; set; }

        [Column("track_id")]
        public Guid? TrackId { get; set; }

        [Column("acronym")]
        public string AcronymText { get; set; } = string.Empty;

        [Column("acronym_type")]
        public string AcronymType { get; set; } = "track";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum AcronymType
    {
        Artist,
        Album,
        Track,
        Single
    }
}
