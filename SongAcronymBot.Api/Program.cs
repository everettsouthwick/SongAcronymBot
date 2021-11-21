using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Api.Services;
using SongAcronymBot.Repository.Data;
using SongAcronymBot.Repository.Repositories;

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
builder.Services.AddTransient<IRedditorRepository,RedditorRepository>();
builder.Services.AddTransient<ISubredditRepository, SubredditRepository>();
builder.Services.AddTransient<ISpotifyService, SpotifyService>();

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
