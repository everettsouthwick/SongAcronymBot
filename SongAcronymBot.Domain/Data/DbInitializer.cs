using SongAcronymBot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongAcronymBot.Domain.Data
{
    public static class DbInitializer
    {
        public static void Initialize(SongAcronymBotContext context)
        {
            context.Database.EnsureCreated();

            if (context.Subreddits.Any())
            {
                return; // DB has been seeded.
            }

            var subreddits = new List<Subreddit>
            {
                new() {Id = "2rlwe", Name = "taylorswift", Enabled = true},
                new() {Id = "39nwj", Name = "popheads", Enabled = true},
                new() {Id = "2wqat", Name = "taylorswiftpictures", Enabled = true},
                new() {Id = "3jka9", Name = "youbelongwithmemes", Enabled = true},
                new() {Id = "yfeq1", Name = "taylorswiftbookclub", Enabled = true},
                new() {Id = "s5e9b", Name = "randomactsofswift", Enabled = true},
                new() {Id = "38sekt", Name = "songacronymbot", Enabled = true},
                new() {Id = "39ou5", Name = "popheadscirclejerk", Enabled = true},
                new() {Id = "3fptd", Name = "lanadelreypictures", Enabled = true},
                new() {Id = "i8bq2", Name = "ariheads", Enabled = true},
                new() {Id = "3mqfo", Name = "billieeilish", Enabled = true},
                new() {Id = "2t3gn", Name = "mariahcarey", Enabled = true},
                new() {Id = "2qnzf", Name = "mileycyrus", Enabled = true},
                new() {Id = "2r4wi", Name = "ladygaga", Enabled = true},
                new() {Id = "2twh8", Name = "carlyraejepsen", Enabled = true},
                new() {Id = "2renz", Name = "paramore", Enabled = true},
                new() {Id = "3acc3", Name = "postmalone", Enabled = true},
                new() {Id = "2x32d", Name = "charlixcx", Enabled = true},
                new() {Id = "2t9fh", Name = "theweeknd", Enabled = true},
                new() {Id = "um430", Name = "rihannaheads", Enabled = true},
                new() {Id = "yd81r", Name = "megantheestallion", Enabled = true},
                new() {Id = "2zwd8", Name = "harrystyles", Enabled = true},
                new() {Id = "2rh4c", Name = "hiphopheads", Enabled = true},
                new() {Id = "zaegj", Name = "lilnasx", Enabled = true},
                new() {Id = "3f8po", Name = "blackpink", Enabled = true},
                new() {Id = "oswpw", Name = "blackpinkmemes", Enabled = true}
            };

            context.Subreddits.AddRange(subreddits);
            context.SaveChanges();

            var redditors = new List<Redditor>
            {
                new() {Id = "8dl49btj", Username = "samueel_", Enabled = false},
                new() {Id = "fb7y2", Username = "spud_simon_salem", Enabled = false},
                new() {Id = "7w8mw", Username = "CommanderWank", Enabled = false},
            };

            foreach (var redditor in redditors)
            {
                context.Redditors.Add(redditor);
            }
            context.SaveChanges();
        }
    }
}
