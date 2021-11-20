using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongAcronymBot.Repository.Data
{
    public class SongAcronymBotContext : DbContext
    {
        public SongAcronymBotContext(DbContextOptions<SongAcronymBotContext> options) : base(options)
        {
        }
        
        public DbSet<Acronym> Acronyms { get; set; }
        public DbSet<Redditor> Redditors { get; set; }
        public DbSet<Subreddit> Subreddits { get; set; }
    }
}
