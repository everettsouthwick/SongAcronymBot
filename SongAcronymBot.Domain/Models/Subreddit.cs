using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace SongAcronymBot.Domain.Supabase.Models
{
    [Table("subreddits")]
    public class Subreddit : BaseModel
    {
        [PrimaryKey("id")]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("min_acronym_length")]
        public int MinAcronymLength { get; set; } = 3;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
