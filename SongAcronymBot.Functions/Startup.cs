using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;
using System.IO;

[assembly: FunctionsStartup(typeof(SongAcronymBot.Functions.Startup))]

namespace SongAcronymBot.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.GetContext().Configuration;

            var debug = config.GetValue<bool>("Debug");

            builder.Services.AddDbContext<SongAcronymBotContext>
                (options =>
                    options.UseSqlServer(debug ? config.GetConnectionString("Local") : config.GetConnectionString("Production"))
                );

            builder.Services.AddTransient<IAcronymRepository, AcronymRepository>();
            builder.Services.AddTransient<IRedditorRepository, RedditorRepository>();
            builder.Services.AddTransient<ISubredditRepository, SubredditRepository>();
            builder.Services.AddTransient<IExcludedRepository, ExcludedRepository>();
            builder.Services.AddTransient<ISpotifyService, SpotifyService>();
        }
    }
}