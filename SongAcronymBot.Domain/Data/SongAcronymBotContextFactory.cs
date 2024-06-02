using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SongAcronymBot.Domain.Data
{
    public class SongAcronymBotContextFactory : IDesignTimeDbContextFactory<SongAcronymBotContext>
    {
        public SongAcronymBotContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SongAcronymBotContext>();

            // Path to the appsettings.json in the SongAcronymBot.Core project
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../SongAcronymBot.Core"))
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("Production");

            optionsBuilder.UseSqlServer(connectionString);

            return new SongAcronymBotContext(optionsBuilder.Options);
        }
    }
}