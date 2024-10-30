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
    options.UseSqlServer(debug ? config.GetConnectionString("Local") : config.GetConnectionString("Production"));
    options.EnableSensitiveDataLogging();
}, ServiceLifetime.Scoped); // Change to scoped lifetime

// Change repositories to scoped lifetime
services.AddScoped<IAcronymRepository, AcronymRepository>();
services.AddScoped<IRedditorRepository, RedditorRepository>();
services.AddScoped<ISubredditRepository, SubredditRepository>();
services.AddScoped<IExcludedRepository, ExcludedRepository>();

// Keep these transient
services.AddTransient<IRedditService, RedditService>();
services.AddTransient<ISpotifyService, SpotifyService>();
services.Configure<SpotifyConfiguration>(config.GetSection("Spotify"));

var serviceProvider = services.BuildServiceProvider();

var redditService = serviceProvider.GetService<IRedditService>();

if (redditService == null)
    throw new NullReferenceException();

var reddit = new RedditClient(
    config["Reddit:AppId"],
    config["Reddit:RefreshToken"],
    config["Reddit:AppSecret"],
    config["Reddit:AccessToken"],
    config["Reddit:UserAgent"]
);

await redditService.StartAsync(reddit, debug);