using Supabase;

namespace SongAcronymBot.Domain.Supabase.Services
{
    /// <summary>
    /// Service for interacting with Supabase
    /// </summary>
    public interface ISupabaseService
    {
        /// <summary>
        /// Gets the Supabase client instance
        /// </summary>
        Client GetClient();

        /// <summary>
        /// Gets the configured schema name for table lookups
        /// </summary>
        string GetSchema();
    }
}
