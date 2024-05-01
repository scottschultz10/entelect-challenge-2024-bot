using SproutReferenceBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Models
{
    public class BotViewCell
    {
        public Location Location { get; set; }
        public Location Index { get; set; }
        public bool HasBot { get; set; }
        public bool IsMe { get; set; }
        public bool HasWeed { get; set; }
        public CellType CellType { get; set; }
        public PowerUpType PowerUpType { get; set; }

        public BotViewCell()
        {
            Location = new();
            Index = new();
        }
    }
}
