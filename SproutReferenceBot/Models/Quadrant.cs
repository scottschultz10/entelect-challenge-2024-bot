using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SproutReferenceBot.Extensions;

namespace SproutReferenceBot.Models
{

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
                return new() { East, NorthEast, North, SouthEast, South };
            }
            else if (quadrant == South)
            {
                return new() { South, SouthEast, East, SouthWest, West };
            }
            else if (quadrant == West)
            {
                return new() { West, NorthWest, North, SouthWest, South };
            }
            else if (quadrant == North)
            {
                return new() { North, NorthEast, East, NorthWest, West };
            }
            else if (quadrant == NorthEast)
            {
                return new() { NorthEast, North, NorthWest, East, SouthEast };
            }
            else if (quadrant == NorthWest)
            {
                return new() { NorthWest, North, NorthEast, West, SouthWest };
            }
            else if (quadrant == SouthEast)
            {
                return new() { SouthEast, South, SouthWest, East, NorthEast };
            }
            else if (quadrant == SouthWest)
            {
                return new() { SouthWest, South, SouthEast, West, NorthWest };
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
            else return NONE;
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
            else return NONE;
        }

    }

}
