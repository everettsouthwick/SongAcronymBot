using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SongAcronymBot.Domain.Supabase.Models
{
    [Table("subreddit_artists")]
    public class SubredditArtist : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("subreddit_id")]
        public Guid SubredditId { get; set; }

        [Column("artist_id")]
        public Guid ArtistId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
