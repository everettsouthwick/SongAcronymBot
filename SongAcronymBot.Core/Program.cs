using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reddit;
using Microsoft.Extensions.Configuration;
using SongAcronymBot.Core.Services;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, true)
    .Build();

var services = new ServiceCollection();

services.AddDbContext<SongAcronymBotContext>
    (options =>
        options.UseSqlServer(config.GetConnectionString("Production"))
    );

services.AddTransient<IAcronymRepository, AcronymRepository>();
services.AddTransient<IRedditorRepository, RedditorRepository>();
services.AddTransient<ISubredditRepository, SubredditRepository>();
services.AddTransient<IRedditService, RedditService>();
services.AddTransient<ISpotifyService, SpotifyService>();

var serviceProvider = services.BuildServiceProvider();

var redditService = serviceProvider.GetService<IRedditService>();
var reddit = new RedditClient
    (config.GetValue<string>("Reddit:AppId"),
    config.GetValue<string>("Reddit:RefreshToken"),
    config.GetValue<string>("Reddit:AppSecret"),
    config.GetValue<string>("Reddit:AccessToken"),
    config.GetValue<string>("Reddit:UserAgent"));

await redditService.StartAsync(reddit);