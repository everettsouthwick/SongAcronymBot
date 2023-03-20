using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<SongAcronymBotContext>
    (options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Production"))
    );

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddTransient<IAcronymRepository, AcronymRepository>();
builder.Services.AddTransient<IRedditorRepository, RedditorRepository>();
builder.Services.AddTransient<ISubredditRepository, SubredditRepository>();
builder.Services.AddTransient<IExcludedRepository, ExcludedRepository>();
builder.Services.AddTransient<ISpotifyService, SpotifyService>();
builder.Services.AddTransient<IBotConfiguration, BotConfiguration>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();