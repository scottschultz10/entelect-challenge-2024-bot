using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Models
{
    public class Location
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Location() { }
        public Location(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object? args)
        {
            if (args == null || args is not Location)
            {
                return false;
            }

            Location compare = (Location)args;

            return X == compare.X && Y == compare.Y;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    public static class LocationDirection
    {
        /// <summary>
        /// (0, 1)
        /// </summary>
        public static readonly Location Right = new(0, 1);
        /// <summary>
        /// (1, 0)
        /// </summary>
        public static readonly Location Down = new(1, 0);
        /// <summary>
        /// (0, -1)
        /// </summary>
        public static readonly Location Left = new(0, -1);
        /// <summary>
        /// (-1, 0)
        /// </summary>
        public static readonly Location Up = new(-1, 0);

        public static Location NextClockwiseDirection(this Location direction)
        {
            if (direction == Right)
            {
                return Down;
            }
            else if (direction == Down)
            {
                return Left;
            }
            else if (direction == Left)
            {
                return Up;
            }
            else if (direction == Up)
            {
                return Right;
            }
            else return direction;
        }

        /// <summary>
        /// Change the location by some magnitude of the location sent in
        /// </summary>
        /// <param name="location">Location data that will increment the X / Y value by some magnitude</param>
        public static Location Move(this Location location, Location direction)
        {
            return new(location.X + direction.X, location.Y + direction.Y);
        }

    }
}
