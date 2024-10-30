using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reddit;
using SongAcronymBot.Core.Services;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection();

bool debug;
if (bool.TryParse(config["Debug"], out bool debugValue))
{
    debug = debugValue;
}
else
{
    debug = false; // Default value if parsing fails
}

services.AddDbContext<SongAcronymBotContext>(options =>
{
    options.UseSqlServer(
        debug ? config.GetConnectionString("Local") : config.GetConnectionString("Production"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    );
}, ServiceLifetime.Scoped); // Changed to Scoped for better concurrency management

// Register repositories as scoped for consistent unit of work pattern
services.AddScoped<IAcronymRepository, AcronymRepository>();
services.AddScoped<IRedditorRepository, RedditorRepository>();
services.AddScoped<ISubredditRepository, SubredditRepository>();
services.AddScoped<IExcludedRepository, ExcludedRepository>();

// Services can remain transient as they don't hold state
services.AddTransient<IRedditService, RedditService>();
services.AddTransient<ISpotifyService, SpotifyService>();
services.Configure<SpotifyConfiguration>(config.GetSection("Spotify"));

var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,
    ValidateOnBuild = true
});

var redditService = serviceProvider.GetRequiredService<IRedditService>(); // Using GetRequiredService instead of GetService

var reddit = new RedditClient(
    config["Reddit:AppId"],
    config["Reddit:RefreshToken"],
    config["Reddit:AppSecret"],
    config["Reddit:AccessToken"],
    config["Reddit:UserAgent"]
);

await redditService.StartAsync(reddit, debug);