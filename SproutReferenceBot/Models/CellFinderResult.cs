using SproutReferenceBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Models
{
    public class CellFinderResult
    {
        public List<CellFinderDirection> Directions;
        public CellFinderPriority Priority;
        public BotViewCell Cell;
        public bool CanCapture = false;
        public bool HasPriority = false;

        public CellFinderResult(BotViewCell cell, CellFinderPriority priority)
        {
            Cell = cell;
            Priority = priority;
            Directions = new();
        }
    }

    public class CellFinderDirection(Location direction, RotationDirection rotation)
    {
        public Location Direction = direction;
        public RotationDirection Rotation = rotation;
    }

    public class CellFinderPriority(int distance, int directionValue)
    {
        public int Distance = distance;
        public int DirectionValue = directionValue;
    }
}
