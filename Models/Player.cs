using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParserLog.Models
{
    public class Player
    {
        public string Name { get; set; }
        public int Kills { get; set; }
        public List<string> OldNames { get; set; }

        public Player()
        {
            OldNames = new List<string>();
        }
    }
}