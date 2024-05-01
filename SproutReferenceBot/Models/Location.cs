using SproutReferenceBot.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;
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

        public static bool operator ==(Location lhs, Location rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(Location lhs, Location rhs)
        {
            return !Equals(lhs, rhs);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// (0, 0)
        /// </summary>
        public static readonly Location NONE = new(0, 0);
    }


    public static class LocationExtensions
    {
        public static Location NextClockwiseDirection(this Location direction)
        {
            if (direction == LocationDirection.Right)
            {
                return LocationDirection.Down;
            }
            else if (direction == LocationDirection.Down)
            {
                return LocationDirection.Left;
            }
            else if (direction == LocationDirection.Left)
            {
                return LocationDirection.Up;
            }
            else if (direction == LocationDirection.Up)
            {
                return LocationDirection.Right;
            }
            else throw new("Direction location not valid");
        }

        public static Location NextCounterClockwiseDirection(this Location direction)
        {
            if (direction == LocationDirection.Right)
            {
                return LocationDirection.Up;
            }
            else if (direction == LocationDirection.Up)
            {
                return LocationDirection.Left;
            }
            else if (direction == LocationDirection.Left)
            {
                return LocationDirection.Down;
            }
            else if (direction == LocationDirection.Down)
            {
                return LocationDirection.Right;
            }
            else throw new("Direction location not valid");
        }

        /// <summary>
        /// Change the location by some magnitude of the location sent in
        /// </summary>
        /// <param name="location">Location data that will increment the X / Y value by some magnitude</param>
        public static Location Move(this Location location, Location direction)
        {
            return new(location.X + direction.X, location.Y + direction.Y);
        }

        /// <summary>
        /// Find the difference between 2 locations, how many x,y squares are between them
        /// </summary>
        public static Location Difference(this Location location, Location destination)
        {
            return new Location(location.X - destination.X, location.Y - destination.Y);
        }

        public static BotAction ToBotAction(this Location direction)
        {
            if (direction == LocationDirection.Right)
            {
                return BotAction.Right;
            }
            else if (direction == LocationDirection.Down)
            {
                return BotAction.Down;
            }
            else if (direction == LocationDirection.Left)
            {
                return BotAction.Left;
            }
            else if (direction == LocationDirection.Up)
            {
                return BotAction.Up;
            }
            else throw new("Direction location not valid");
        }

        public static Location ToLocationDirection(this BotAction action)
        {
            if (action == BotAction.Right)
            {
                return LocationDirection.Right;
            }
            else if (action == BotAction.Down)
            {
                return LocationDirection.Down;
            }
            else if (action == BotAction.Left)
            {
                return LocationDirection.Left;
            }
            else if (action == BotAction.Up)
            {
                return LocationDirection.Up;
            }
            else throw new("Bot Action not valid");
        }

        public static int DistanceTo(this Location from, Location to)
        {
            return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
        }

        /// <summary>
        /// Lower is better
        /// </summary>
        public static int DirectionPriority(this Location location, Location destination, BotAction currentDirection)
        {
            Location difference = location.Difference(destination);

            if (currentDirection == BotAction.Up)
            {
                //don't want to move down (don't want to increase the X value)
                if (difference.X < 0)
                {
                    return 1;
                }
                else if (difference.X > 0)
                {
                    //keep going up
                    return -1;
                }
                else return 0;
            }
            else if (currentDirection == BotAction.Down)
            {
                //don't want to move up (don't want to decrease the X value)
                if (difference.X > 0)
                {
                    return 1;
                }
                else if (difference.X < 0)
                {
                    //keep going down
                    return -1;
                }
                else return 0;
            }
            else if (currentDirection == BotAction.Left)
            {
                //don't want to move right (don't want to increase the X value)
                if (difference.Y < 0)
                {
                    return 1;
                }
                if (difference.Y > 0)
                {
                    //keep going left
                    return -1;
                }
                else return 0;
            }
            else if (currentDirection == BotAction.Right)
            {
                //don't want to move left (don't want to decrease the X value)
                if (difference.Y > 0)
                {
                    return 1;
                }
                else if (difference.Y < 0)
                {
                    return -1;
                }
                else return 0;
            }
            else return 0;
        }
    }
}
