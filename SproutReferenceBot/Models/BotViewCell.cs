using SproutReferenceBot.Enums;
using SproutReferenceBot.Globals;
using SproutReferenceBot.Services;
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
        public bool HasBot { get; set; }
        public bool IsMe { get; set; }
        public bool HasWeed { get; set; }
        public CellType CellType { get; set; }
        public PowerUpType PowerUpType { get; set; }

        public bool IsHazard => HasWeed || (HasBot && !IsMe);

        public bool IsTrail => 4 <= (int)CellType && (int)CellType <= 7;

        /// <summary>
        /// Is Territory cell type but is not mine
        /// </summary>
        public bool IsEnemyTerritory => 0 <= (int)CellType && (int)CellType <= 3 && CellType != BotServiceGlobals.MyTerritory;

        public BotViewCell()
        {
            Location = new();
        }
    }
}
