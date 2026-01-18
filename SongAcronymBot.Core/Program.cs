using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reddit;
using SongAcronymBot.Core.Services;
using SongAcronymBot.Core.Services.Interfaces;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Repositories.Interfaces;
using SongAcronymBot.Domain.Services;
using SongAcronymBot.Domain.Services.Interfaces;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(config);
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
services.AddScoped<IPromotionalMessageRepository, PromotionalMessageRepository>();

// Core services (SOLID refactored)
services.AddSingleton<IOptOutManager, OptOutManager>();
services.AddSingleton<ISubredditAcronymCache, SubredditAcronymCache>();
services.AddTransient<IReplyFormatter, ReplyFormatter>();
services.AddTransient<IAcronymMatcher, AcronymMatcher>();
services.AddTransient<IMessageProcessor, MessageProcessor>();
services.AddTransient<ICommentProcessor, CommentProcessor>();
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