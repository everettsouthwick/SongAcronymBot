using SongAcronymBot.Domain.Supabase.Models;

namespace SongAcronymBot.Domain.Repositories.Interfaces
{
    public interface IPromotionalMessageRepository : IBaseRepository<PromotionalMessage>
    {
        /// <summary>
        /// Gets all active promotional messages.
        /// </summary>
        Task<List<PromotionalMessage>> GetActiveMessagesAsync();

        /// <summary>
        /// Gets a random promotional message from active messages.
        /// </summary>
        Task<PromotionalMessage?> GetRandomActiveMessageAsync();
    }
}
