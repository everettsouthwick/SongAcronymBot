using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reddit;
using SongAcronymBot.Core.Services;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Repositories;
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

services.AddDbContext<SongAcronymBotContext>(options =>
    options.UseSqlServer(config.GetConnectionString("Production"))
);

// Legacy EF Core repositories
services.AddTransient<SongAcronymBot.Domain.Repositories.IAcronymRepository, SongAcronymBot.Domain.Repositories.AcronymRepository>();
services.AddTransient<IRedditService, RedditService>();

// Supabase services
services.AddSingleton<ISupabaseService, SupabaseService>();

// Supabase repositories
services.AddScoped<SongAcronymBot.Domain.Supabase.Repositories.IOptedOutRedditorRepository, SongAcronymBot.Domain.Supabase.Repositories.OptedOutRedditorRepository>();

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