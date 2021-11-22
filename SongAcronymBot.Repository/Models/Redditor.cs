using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongAcronymBot.Domain.Models
{
    public class Redditor
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public bool Enabled { get; set; }
    }
}
