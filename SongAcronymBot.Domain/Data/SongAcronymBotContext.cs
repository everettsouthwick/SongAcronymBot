using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongAcronymBot.Domain.Data
{
    public class SongAcronymBotContext(DbContextOptions<SongAcronymBotContext> options) : DbContext(options)
    {
        public DbSet<Acronym> Acronyms { get; set; }
        public DbSet<Redditor> Redditors { get; set; }
        public DbSet<Subreddit> Subreddits { get; set; }
    }
}