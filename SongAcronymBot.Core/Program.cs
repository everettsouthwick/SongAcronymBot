using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reddit;
using SongAcronymBot.Core.Services;
using SongAcronymBot.Domain.Supabase.Repositories;
using SongAcronymBot.Domain.Supabase.Services;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection();

// Register IConfiguration for services that need it
services.AddSingleton<IConfiguration>(config);

// Add logging from configuration
services.AddLogging(builder =>
{
    builder.AddConfiguration(config.GetSection("Logging"));
    builder.AddConsole();
});

// Supabase services
services.AddSingleton<ISupabaseService, SupabaseService>();

// Supabase repositories
services.AddScoped<IOptedOutRedditorRepository, OptedOutRedditorRepository>();
services.AddScoped<IAcronymRepository, AcronymRepository>();
services.AddScoped<IArtistRepository, ArtistRepository>();
services.AddScoped<IAlbumRepository, AlbumRepository>();
services.AddScoped<ITrackRepository, TrackRepository>();
services.AddScoped<ISubredditRepository, SubredditRepository>();
services.AddScoped<ISubredditArtistRepository, SubredditArtistRepository>();

services.AddTransient<IRedditService, RedditService>();

var serviceProvider = services.BuildServiceProvider();

var redditService = serviceProvider.GetService<IRedditService>() ?? throw new NullReferenceException();

var reddit = new RedditClient(
    config["Reddit:AppId"],
    config["Reddit:RefreshToken"],
    config["Reddit:AppSecret"],
    config["Reddit:AccessToken"],
    config["Reddit:UserAgent"]
);

await redditService.StartAsync(reddit);