using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<SongAcronymBotContext>(options =>
        {
            var debug = context.Configuration.GetValue<bool>("Debug");
            var local = context.Configuration.GetConnectionString("Local");
            var production = context.Configuration.GetConnectionString("Production");

            options.UseSqlServer(debug ? context.Configuration.GetConnectionString("Local") : context.Configuration.GetConnectionString("Production"));
        });

        services.AddTransient<IAcronymRepository, AcronymRepository>();
        services.AddTransient<IRedditorRepository, RedditorRepository>();
        services.AddTransient<ISubredditRepository, SubredditRepository>();
        services.AddTransient<IExcludedRepository, ExcludedRepository>();
        services.AddTransient<ISpotifyService, SpotifyService>();
        services.Configure<SpotifyConfiguration>(context.Configuration.GetSection("Spotify"));
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();