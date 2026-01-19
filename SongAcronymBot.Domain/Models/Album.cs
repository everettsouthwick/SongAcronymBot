using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SongAcronymBot.Domain.Supabase.Models
{
    [Table("albums")]
    public class Album : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("artist_id")]
        public Guid ArtistId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("slug")]
        public string Slug { get; set; } = string.Empty;

        [Column("spotify_album_id")]
        public string? SpotifyAlbumId { get; set; }

        [Column("spotify_url")]
        public string? SpotifyUrl { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("year_released")]
        public int? YearReleased { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
