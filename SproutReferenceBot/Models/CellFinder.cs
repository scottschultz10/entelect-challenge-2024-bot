using SproutReferenceBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Models
{
    public static class CellFinder
    {
        /// <summary>
        /// Build a 1 dimensional list of cellviews starting from the bot and spiralling outwards
        /// </summary>
        /// <returns>A 1 dimensional list of cellviews that gradually get further from the bot as the list continues</returns>
        public static List<CellView> GetClockwiseView(List<List<CellView>> botView)
        {
            if (botView.Count == 0) return new();

            List<CellView> clockwiseView = new() { botView[4][4] };

            Location currentIndex = new(4, 5);
            Location currentDirection = LocationDirection.Down;
            int directionMax = 1;
            int directionCount = 0;

            while (currentIndex.X < botView.Count && currentIndex.Y < botView[0].Count)
            {
                clockwiseView.Add(botView[currentIndex.X][currentIndex.Y]);

                if (directionCount >= directionMax)
                {
                    //reset the count
                    directionCount = 0;

                    //change the direction
                    currentDirection = currentDirection.NextClockwiseDirection();

                    //increment the direction max on left and right
                    if (currentDirection == LocationDirection.Left || currentDirection == LocationDirection.Right)
                    {
                        directionMax++;
                    }
                }

                directionCount++;
                currentIndex = currentIndex.Move(currentDirection);
            }

            return clockwiseView;
        }

        /// <summary>
        /// From a 1d view find the closest corner to the bot. A corner is defined as 2 adjacent cells of myTerritory and the remaining 2 adjacent cells of another cell type)
        /// </summary>
        /// <param name="clockwiseView">1 dimensional list of the cell view. from GetClockwiseView</param>
        /// <param name="myTerritory">The CellType that defines my bots territory</param>
        /// <returns></returns>
        public static CellView? FindCornerCell(List<List<CellView>> botView, List<CellView> clockwiseView, CellType myTerritory)
        {
            int xCount = botView.Count;
            int yCount = botView[0].Count;

            foreach (CellView cell in clockwiseView)
            {
                Location rightIndex = cell.Index.Move(LocationDirection.Right);
                Location downIndex = cell.Index.Move(LocationDirection.Down);
                Location leftIndex = cell.Index.Move(LocationDirection.Left);
                Location upIndex = cell.Index.Move(LocationDirection.Up);

                //validate that all cells are within the view
                if (!IsIndexInBounds(rightIndex, xCount, yCount)
                    || !IsIndexInBounds(downIndex, xCount, yCount)
                    || !IsIndexInBounds(leftIndex, xCount, yCount)
                    || !IsIndexInBounds(leftIndex, xCount, yCount))
                {
                    //the directions are no longer in bounds. There are no corners within view
                    return null;
                }

                CellType rightType = botView[rightIndex.X][rightIndex.Y].CellType;
                CellType downType = botView[downIndex.X][downIndex.Y].CellType;
                CellType leftType = botView[leftIndex.X][leftIndex.Y].CellType;
                CellType upType = botView[upIndex.X][upIndex.Y].CellType;

                //check that this cell has two adjacent cells that are myTerritory and the other 2 are not
                if ((rightType == myTerritory && downType == myTerritory && leftType != myTerritory && upType != myTerritory)
                    || (rightType != myTerritory && downType == myTerritory && leftType == myTerritory && upType != myTerritory)
                    || (rightType != myTerritory && downType != myTerritory && leftType == myTerritory && upType == myTerritory)
                    || (rightType == myTerritory && downType != myTerritory && leftType != myTerritory && upType == myTerritory))
                {
                    return cell;
                }
            }

            return null;
        }

        /// <summary>
        /// Check that the x and y values are within the range of: above or equal to 0 and below the Count of the list
        /// </summary>
        /// <returns>True if the index is valid and in bounds</returns>
        private static bool IsIndexInBounds(Location index, int xCount, int yCount)
        {
            return (0 <= index.X && index.X < xCount) && (0 <= index.Y && index.Y < yCount);
        }

    }
}
