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

        public static bool operator ==(Location? lhs, Location? rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(Location? lhs, Location? rhs)
        {
            return !Equals(lhs, rhs);
        }

        public override string ToString()
        {
            PropertyInfo[] properties = typeof(LocationQuadrant).GetProperties(BindingFlags.Public | BindingFlags.Static);

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == typeof(Location) && ((Location?)property.GetValue(null) ?? LocationDirection.NONE) == this)
                {
                    return $"LocationQuadrant.{property.Name}, ({X}, {Y})";
                }
            }

            return $"({X}, {Y})";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
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

    
}
