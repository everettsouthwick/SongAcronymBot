using SongAcronymBot.Domain.Repositories.Interfaces;
using SongAcronymBot.Domain.Services.Interfaces;
using SongAcronymBot.Domain.Supabase.Models;
using static Supabase.Postgrest.Constants;

namespace SongAcronymBot.Domain.Repositories
{
    public class PromotionalMessageRepository(ISupabaseService supabaseService) : BaseRepository<PromotionalMessage>(supabaseService), IPromotionalMessageRepository
    {
        private static readonly Random _random = new();

        public async Task<List<PromotionalMessage>> GetActiveMessagesAsync()
        {
            var response = await GetQueryBuilder()
                .Filter("is_active", Operator.Equals, "true")
                .Get();

            return response.Models;
        }

        public async Task<PromotionalMessage?> GetRandomActiveMessageAsync()
        {
            var activeMessages = await GetActiveMessagesAsync();

            if (activeMessages.Count == 0)
            {
                return null;
            }

            // Weighted random selection based on weight field
            var totalWeight = activeMessages.Sum(m => m.Weight);
            var randomValue = _random.Next(totalWeight);
            var cumulativeWeight = 0;

            foreach (var message in activeMessages)
            {
                cumulativeWeight += message.Weight;
                if (randomValue < cumulativeWeight)
                {
                    return message;
                }
            }

            // Fallback to first message (should not reach here)
            return activeMessages.FirstOrDefault();
        }
    }
}
