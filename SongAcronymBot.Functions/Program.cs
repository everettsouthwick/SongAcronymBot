using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;
using SongAcronymBot.Domain.Supabase.Services;

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

        // Legacy EF Core repositories
        services.AddTransient<SongAcronymBot.Domain.Repositories.IAcronymRepository, SongAcronymBot.Domain.Repositories.AcronymRepository>();
        services.AddTransient<IRedditorRepository, RedditorRepository>();
        services.AddTransient<SongAcronymBot.Domain.Repositories.ISubredditRepository, SongAcronymBot.Domain.Repositories.SubredditRepository>();
        services.AddTransient<IExcludedRepository, ExcludedRepository>();
        services.AddTransient<ISpotifyService, SpotifyService>();
        services.Configure<SpotifyConfiguration>(context.Configuration.GetSection("Spotify"));

        // Supabase services
        services.AddSingleton<ISupabaseService, SupabaseService>();

        // Supabase repositories
        services.AddScoped<SongAcronymBot.Domain.Supabase.Repositories.IArtistRepository, SongAcronymBot.Domain.Supabase.Repositories.ArtistRepository>();
        services.AddScoped<SongAcronymBot.Domain.Supabase.Repositories.IAlbumRepository, SongAcronymBot.Domain.Supabase.Repositories.AlbumRepository>();
        services.AddScoped<SongAcronymBot.Domain.Supabase.Repositories.ITrackRepository, SongAcronymBot.Domain.Supabase.Repositories.TrackRepository>();
        services.AddScoped<SongAcronymBot.Domain.Supabase.Repositories.IAcronymRepository, SongAcronymBot.Domain.Supabase.Repositories.AcronymRepository>();
        services.AddScoped<SongAcronymBot.Domain.Supabase.Repositories.ISubredditRepository, SongAcronymBot.Domain.Supabase.Repositories.SubredditRepository>();
        services.AddScoped<SongAcronymBot.Domain.Supabase.Repositories.ISubredditArtistRepository, SongAcronymBot.Domain.Supabase.Repositories.SubredditArtistRepository>();
        services.AddScoped<SongAcronymBot.Domain.Supabase.Repositories.IOptedOutRedditorRepository, SongAcronymBot.Domain.Supabase.Repositories.OptedOutRedditorRepository>();
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();