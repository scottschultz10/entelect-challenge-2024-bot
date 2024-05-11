﻿using SproutReferenceBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Models
{
    public class MovementQueueItem
    {
        public Location Destination { get; set; }
        public CellFinderDirection Direction { get; set; }

        public MovementQueueItem(Location destination, CellFinderDirection direction)
        {
            Destination = destination;
            Direction = direction;
        }
    }

    public class MovementAction(BotAction action, Location location)
    {
        public BotAction Action = action;
        public Location Location = location;

        public BotViewCell? GetBotViewCell(BotView botView)
        {
            return botView.CellByLocation(Location);
        }

        public override string ToString()
        {
            return $"{Action}, Cell: {Location}";
        }
    }

}
