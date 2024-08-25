using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SproutReferenceBot.Enums;
using SproutReferenceBot.Globals;
using SproutReferenceBot.Models;


namespace SproutReferenceBot.Extensions
{
    internal static class BotViewExtensions
    {
        public static List<BotViewCell> ToBotViewCells(this IEnumerable<MovementAction> movementActions)
        {
            return movementActions.Select(x => x.Location.ToBotViewCell()).Where(x => x != null).Select(x => x!).ToList();
        }

        /// <summary>
        /// Send in a location value and find the associated cell in the BotView
        /// </summary>
        /// <param name="location"></param>
        /// <returns>The BotViewCell associated with the sent in location. If no location is found return null</returns>
        public static BotViewCell? ToBotViewCell(this Location location)
        {
            if (BotViewGlobals.EntireView.TryGetValue(location, out (BotViewCell Cell, int Tick) cell))
            {
                //check the age of the cell. Do not return old values
                //out of bounds will never change
                if (BotServiceGlobals.GameTick <= (cell.Tick + 20) || cell.Cell.CellType == CellType.OutOfBounds)
                {
                    return cell.Cell;
                }
                else return null;
            }
            else return null;
        }

        
        public static List<BotViewCell> CellBuffer(this Location location)
        {
            List<Location> allBuffers =
            [
                LocationQuadrant.East,
                LocationQuadrant.South,
                LocationQuadrant.West,
                LocationQuadrant.North,
                LocationQuadrant.NorthEast,
                LocationQuadrant.NorthWest,
                LocationQuadrant.SouthEast,
                LocationQuadrant.SouthWest,
                LocationQuadrant.NONE,
            ];

            List<BotViewCell> returnList = [];
            foreach (Location buffer in allBuffers)
            {
                BotViewCell? bufferCell = location.Move(buffer).ToBotViewCell();
                if (bufferCell != null)
                {
                    returnList.Add(bufferCell);
                }
            }

            return returnList;
        }

        /// <summary>
        /// Buffer that only contains the Right, Down, Left, Up values 
        /// </summary>
        /// <returns></returns>
        public static List<BotViewCell> CellPrimaryBuffer(this Location location)
        {
            List<Location> allBuffers =
            [
                LocationQuadrant.East,
                LocationQuadrant.South,
                LocationQuadrant.West,
                LocationQuadrant.North,
            ];

            List<BotViewCell> returnList = [];
            foreach (Location buffer in allBuffers)
            {
                BotViewCell? bufferCell = location.Move(buffer).ToBotViewCell();
                if (bufferCell != null)
                {
                    returnList.Add(bufferCell);
                }
            }

            return returnList;
        }

    }
}
