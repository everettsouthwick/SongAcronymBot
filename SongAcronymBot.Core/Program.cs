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
    options.UseSqlServer(debug ? config.GetConnectionString("Local") : config.GetConnectionString("Production"))
);

services.AddTransient<IAcronymRepository, AcronymRepository>();
services.AddTransient<IRedditorRepository, RedditorRepository>();
services.AddTransient<ISubredditRepository, SubredditRepository>();
services.AddTransient<IRedditService, RedditService>();
services.AddTransient<ISpotifyService, SpotifyService>();
services.AddTransient<IExcludedRepository, ExcludedRepository>();
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