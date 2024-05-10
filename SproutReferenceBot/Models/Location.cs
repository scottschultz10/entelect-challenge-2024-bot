using SproutReferenceBot.Enums;
using System.Reflection;


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
            PropertyInfo[] properties = typeof(LocationQuadrant).GetProperties(BindingFlags.Public | BindingFlags.Static);

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == typeof(Location) && (Location?)property.GetValue(null) == this)
                {
                    return $"LocationQuadrant.{property.Name}, ({X}, {Y})";
                }
            }

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
        /// (1, 0)
        /// </summary>
        public static readonly Location Right = new(1, 0);
        /// <summary>
        /// (0, 1)
        /// </summary>
        public static readonly Location Down = new(0, 1);
        /// <summary>
        /// (-1, 0)
        /// </summary>
        public static readonly Location Left = new(-1, 0);
        /// <summary>
        /// (0, -1)
        /// </summary>
        public static readonly Location Up = new(0, -1);

        /// <summary>
        /// (0, 0)
        /// </summary>
        public static readonly Location NONE = new(0, 0);
    }

    
    public static class LocationQuadrant
    {
        /// <summary>
        /// (1, 0)
        /// </summary>
        public static Location East { get { return new(1, 0); } }
        /// <summary>
        /// (0, 1)
        /// </summary>
        public static Location South { get { return new(0, 1); } }
        /// <summary>
        /// (-1, 0)
        /// </summary>
        public static Location West { get { return new(-1, 0); } }
        /// <summary>
        /// (0, -1)
        /// </summary>
        public static Location North { get { return new(0, -1); } }
        /// <summary>
        /// (1, -1)
        /// </summary>
        public static Location NorthEast { get { return new(1, -1); } }
        /// <summary>
        /// (-1, -1)
        /// </summary>
        public static Location NorthWest { get { return new(-1, -1); } }
        /// <summary>
        /// (1, 1)
        /// </summary>
        public static Location SouthEast { get { return new(1, 1); } }
        /// <summary>
        /// (1, -1)
        /// </summary>
        public static Location SouthWest { get { return new(-1, 1); } }

        public static Location NONE { get { return new(0, 0); } }

        public static Location DestinationQuadrant(this Location location, Location destination)
        {
            //inverse difference
            Location difference = destination.Difference(location);

            //convert into the predefined locations
            int quadX = difference.X != 0 ? difference.X / Math.Abs(difference.X) : 0;
            int quadY = difference.Y != 0 ? difference.Y / Math.Abs(difference.Y) : 0;

            return new Location(quadX, quadY);
        }

        private static List<Location> NeighbouringQuadrants(this Location quadrant)
        {
            if (quadrant == East)
            {
                return new() { East, NorthEast, SouthEast };
            }
            else if (quadrant == South)
            {
                return new() { South, SouthEast, SouthWest };
            }
            else if (quadrant == West)
            {
                return new() { West, NorthWest, SouthWest };
            }
            else if (quadrant == North)
            {
                return new() { North, NorthEast, NorthWest };
            }
            else if (quadrant == NorthEast)
            {
                return new() { NorthEast, North, East };
            }
            else if (quadrant == NorthWest)
            {
                return new() { NorthWest, North, West };
            }
            else if (quadrant == SouthEast)
            {
                return new() { SouthEast, South, East };
            }
            else if (quadrant == SouthWest)
            {
                return new() { SouthWest, South, West };
            }
            else return new();
        }

        public static bool IsDestinationInNeighbouringQuadrant(this Location location, Location destination, Location quadrant)
        {
            Location destinationQuadrant = location.DestinationQuadrant(destination);

            return quadrant.NeighbouringQuadrants().Contains(destinationQuadrant);
        }

        public static Location NextClockwiseQuadrant(this Location quadrant)
        {
            if (quadrant == East)
            {
                return SouthEast;
            }
            else if (quadrant == SouthEast)
            {
                return South;
            }
            else if (quadrant == South)
            {
                return SouthWest;
            }
            else if (quadrant == SouthWest)
            {
                return West;
            }
            if (quadrant == West)
            {
                return NorthWest;
            }
            else if (quadrant == NorthWest)
            {
                return North;
            }
            else if (quadrant == North)
            {
                return NorthEast;
            }
            else if (quadrant == NorthEast)
            {
                return East;
            }
            else throw new("Direction location not valid");
        }

        public static Location NextCounterClockwiseQuadrant(this Location quadrant)
        {
            if (quadrant == East)
            {
                return NorthEast;
            }
            else if (quadrant == NorthEast)
            {
                return North;
            }
            else if (quadrant == North)
            {
                return NorthWest;
            }
            else if (quadrant == NorthWest)
            {
                return West;
            }
            if (quadrant == West)
            {
                return SouthWest;
            }
            else if (quadrant == SouthWest)
            {
                return South;
            }
            else if (quadrant == South)
            {
                return SouthEast;
            }
            else if (quadrant == SouthEast)
            {
                return East;
            }
            else throw new("Direction location not valid");
        }

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
        public static Location Move(this Location location, Location direction, int magnitude = 1)
        {
            return new(location.X + (direction.X * magnitude), location.Y + (direction.Y * magnitude));
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
                if (difference.Y < 0)
                {
                    return 1;
                }
                else if (difference.Y > 0)
                {
                    //keep going up
                    return -1;
                }
                else return -2;
            }
            else if (currentDirection == BotAction.Down)
            {
                //don't want to move up (don't want to decrease the X value)
                if (difference.Y > 0)
                {
                    return 1;
                }
                else if (difference.Y < 0)
                {
                    //keep going down
                    return -1;
                }
                else return -2;
            }
            else if (currentDirection == BotAction.Left)
            {
                //don't want to move right (don't want to increase the X value)
                if (difference.X < 0)
                {
                    return 1;
                }
                else if (difference.X > 0)
                {
                    //keep going left
                    return -1;
                }
                else return -2;
            }
            else if (currentDirection == BotAction.Right)
            {
                //don't want to move left (don't want to decrease the X value)
                if (difference.X > 0)
                {
                    return 1;
                }
                else if (difference.X < 0)
                {
                    return -1;
                }
                else return -2;
            }
            else return 0;
        }

        
        /// <summary>
        /// Get the direction that has the most influence between two locations
        /// </summary>
        /// <returns></returns>
        public static Location CommonDirection(this Location location, Location destination)
        {
            Location difference = location.Difference(destination);

            int yCommon = Math.Abs(difference.Y);
            Location yDirection;
            if (difference.Y > 0)
            {
                //need to decrease the difference so go up
                yDirection = LocationDirection.Up;
            }
            else //if (difference.Y < 0)
            {
                //need to increase the difference so go down
                yDirection = LocationDirection.Down;
            }

            int xCommon = Math.Abs(difference.X);
            Location xDirection;
            if (difference.X > 0)
            {
                //need to decrease the difference so go left
                xDirection = LocationDirection.Left;
            }
            else //if (difference.X < 0)
            {
                xDirection = LocationDirection.Right;
            }

            if (yCommon > xCommon)
            {
                return yDirection;
            }
            else
            {
                return xDirection;
            }
        }

        public static RotationDirection RotationFromDestination(this Location location, Location destination)
        {
            //use inverse to get actual directions
            Location difference = destination.Difference(location);

            //only one direction to go - default
            if (difference.X == 0 || difference.Y == 0)
            {
                return RotationDirection.Clockwise;
            }

            //prioritise shortest movement then long

            //X greater, has more influence
            if (Math.Abs(difference.X) > Math.Abs(difference.Y))
            {
                if (difference.X > 0)
                {
                    if (difference.Y > 0)
                    {
                        //Y is down
                        return RotationDirection.Clockwise;
                    }
                    else if (difference.Y < 0)
                    {
                        //Y is up
                        return RotationDirection.CounterClockwise;
                    }
                }
                else if (difference.X < 0)
                {
                    if (difference.Y > 0)
                    {
                        //Y is down
                        return RotationDirection.CounterClockwise;
                    }
                    else if (difference.Y < 0)
                    {
                        //Y is up
                        return RotationDirection.Clockwise;
                    }
                }
            }
            //Y greater, has more influence
            else if (Math.Abs(difference.X) < Math.Abs(difference.Y))
            {
                if (difference.Y > 0)
                {
                    if (difference.X > 0)
                    {
                        //X is right
                        return RotationDirection.CounterClockwise;
                    }
                    else if (difference.X < 0)
                    {
                        //X is left
                        return RotationDirection.Clockwise;
                    }
                }
                else if (difference.Y < 0)
                {
                    if (difference.X > 0)
                    {
                        //X is right
                        return RotationDirection.Clockwise;
                    }
                    else if (difference.X < 0)
                    {
                        //X is left
                        return RotationDirection.CounterClockwise;
                    }
                }
            }

            //only one direction
            return RotationDirection.Clockwise;
        }
    }
}
