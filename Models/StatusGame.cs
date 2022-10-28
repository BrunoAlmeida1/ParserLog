using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParserLog.Models
{
    public class StatusGame
    {
        public int totalKills { get; set; }
        public List<Player> Players { get; set; }

        public StatusGame()
        {
            Players = new List<Player>();
        }
    }
}