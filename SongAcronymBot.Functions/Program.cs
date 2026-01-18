using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;
using SongAcronymBot.Domain.Supabase.Services;
using SongAcronymBot.Domain.Supabase.Repositories;

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
        services.AddTransient<IAcronymRepository, AcronymRepository>();
        services.AddTransient<IRedditorRepository, RedditorRepository>();
        services.AddTransient<ISubredditRepository, SubredditRepository>();
        services.AddTransient<IExcludedRepository, ExcludedRepository>();
        services.AddTransient<ISpotifyService, SpotifyService>();
        services.Configure<SpotifyConfiguration>(context.Configuration.GetSection("Spotify"));

        // Supabase services
        services.AddSingleton<ISupabaseService, SupabaseService>();

        // Supabase repositories
        services.AddScoped<Domain.Supabase.Repositories.IArtistRepository, Domain.Supabase.Repositories.ArtistRepository>();
        services.AddScoped<Domain.Supabase.Repositories.IAlbumRepository, Domain.Supabase.Repositories.AlbumRepository>();
        services.AddScoped<Domain.Supabase.Repositories.ITrackRepository, Domain.Supabase.Repositories.TrackRepository>();
        services.AddScoped<Domain.Supabase.Repositories.IAcronymRepository, Domain.Supabase.Repositories.AcronymRepository>();
        services.AddScoped<Domain.Supabase.Repositories.ISubredditRepository, Domain.Supabase.Repositories.SubredditRepository>();
        services.AddScoped<Domain.Supabase.Repositories.ISubredditArtistRepository, Domain.Supabase.Repositories.SubredditArtistRepository>();
        services.AddScoped<Domain.Supabase.Repositories.IOptedOutRedditorRepository, Domain.Supabase.Repositories.OptedOutRedditorRepository>();
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();