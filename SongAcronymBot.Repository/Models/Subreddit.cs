using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongAcronymBot.Repository.Models
{
    public class Subreddit
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool Enabled { get; set; }
    }
}
