using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SongAcronymBot.Domain.Services.Interfaces;
using Supabase;

namespace SongAcronymBot.Domain.Services
{
    /// <summary>
    /// Implementation of the Supabase service
    /// </summary>
    public class SupabaseService : ISupabaseService
    {
        private readonly Client _supabaseClient;
        private readonly ILogger<SupabaseService> _logger;
        private readonly string _schema;

        /// <summary>
        /// Initializes a new instance of the SupabaseService class
        /// </summary>
        /// <param name="configuration">The configuration to get Supabase URL and key</param>
        /// <param name="logger">The logger</param>
        public SupabaseService(IConfiguration configuration, ILogger<SupabaseService> logger)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            string url = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url configuration is missing");
            string key = configuration["Supabase:Key"] ?? throw new InvalidOperationException("Supabase:Key configuration is missing");
            string schema = configuration["Supabase:Schema"] ?? "public";

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
                Schema = schema
            };

            _supabaseClient = new Client(url, key, options);
            _schema = schema;
        }

        /// <inheritdoc/>
        public Client GetClient()
        {
            return _supabaseClient;
        }

        /// <inheritdoc/>
        public string GetSchema()
        {
            return _schema;
        }
    }
}
