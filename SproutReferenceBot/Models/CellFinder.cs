using SproutReferenceBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Models
{
    public class CellFinderResult
    {
        public List<CellFinderDirection> Directions;
        public CellFinderPriority Priority;
        public BotViewCell Cell;

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

    public static class CellFinder
    {
        /// <summary>
        /// Build a 1 dimensional list of cellviews starting from the bot and spiralling outwards
        /// </summary>
        /// <returns>A 1 dimensional list of cellviews that gradually get further from the bot as the list continues</returns>
        public static List<BotViewCell> GetClockwiseView(List<List<BotViewCell>> botView)
        {
            if (botView.Count == 0) return new();

            List<BotViewCell> clockwiseView = new() { botView[(botView.Count - 1) / 2][(botView[0].Count - 1) / 2] };

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
        /// <returns>Null if no corner found in the botView</returns>
        public static CellFinderResult? FindCornerCell(BotView botView, List<BotViewCell> clockwiseView, CellType myTerritory, BotAction currentDirection)
        {
            int xCount = botView.Cells.Count;
            int yCount = botView.Cells[0].Count;

            BotViewCell centerCell = botView.GetCenterCell();

            //find multiple corners and prioritise
            List<CellFinderResult> allCorners = new();

            foreach (BotViewCell cell in clockwiseView)
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
                    break;
                }

                CellType rightType = botView.Cells[rightIndex.X][rightIndex.Y].CellType;
                CellType downType = botView.Cells[downIndex.X][downIndex.Y].CellType;
                CellType leftType = botView.Cells[leftIndex.X][leftIndex.Y].CellType;
                CellType upType = botView.Cells[upIndex.X][upIndex.Y].CellType;

                CellType[] validateSideTypes = { rightType, downType, leftType, upType };
                /*
                 * Not valid when:
                 * if 2 of the sides are out of bounds. Corner of map
                 * OR if there are not exactly 2 sides of myTerritory
                 */ 
                if (validateSideTypes.Count(x => x == CellType.OutOfBounds) >= 2
                    || validateSideTypes.Count(x => x == myTerritory) != 2)
                {
                    continue;
                }

                CellFinderPriority priority = new(centerCell.Location.DistanceTo(cell.Location), centerCell.Location.DirectionPriority(cell.Location, currentDirection));

                CellFinderResult cellFinder = new(cell, priority);

                //check that this cell has two adjacent cells that are myTerritory and the other 2 are not
                if (rightType == myTerritory && downType == myTerritory && leftType != myTerritory && upType != myTerritory)
                {
                    cellFinder.Directions = new() 
                    {
                        new CellFinderDirection(LocationDirection.Up, RotationDirection.Clockwise),
                        new CellFinderDirection(LocationDirection.Left, RotationDirection.CounterClockwise)
                    };

                    //right-down corner
                    allCorners.Add(cellFinder);
                }
                else if (rightType != myTerritory && downType == myTerritory && leftType == myTerritory && upType != myTerritory)
                {
                    //down-left corner
                    cellFinder.Directions = new()
                    {
                        new CellFinderDirection(LocationDirection.Right, RotationDirection.Clockwise),
                        new CellFinderDirection(LocationDirection.Up, RotationDirection.CounterClockwise),
                    };

                    allCorners.Add(cellFinder);
                }
                else if (rightType != myTerritory && downType != myTerritory && leftType == myTerritory && upType == myTerritory)
                {
                    //left-up corner
                    cellFinder.Directions = new()
                    {
                        new CellFinderDirection(LocationDirection.Down, RotationDirection.Clockwise),
                        new CellFinderDirection(LocationDirection.Right, RotationDirection.CounterClockwise),
                    };

                    allCorners.Add(cellFinder);
                }
                else if (rightType == myTerritory && downType != myTerritory && leftType != myTerritory && upType == myTerritory)
                {
                    //up-right corner
                    cellFinder.Directions = new()
                    {
                        new CellFinderDirection(LocationDirection.Left, RotationDirection.Clockwise),
                        new CellFinderDirection(LocationDirection.Down, RotationDirection.CounterClockwise),
                    };

                    allCorners.Add(cellFinder);
                }
            }

            //standing on corner cell / return that
            if (allCorners.Any(x => x.Cell.Location == centerCell.Location))
            {
                return allCorners.First(x => x.Cell.Location == centerCell.Location);
            }
            else if (allCorners.Count > 1)
            {
                //group corners with the same priorities
                //then pick a random corner from that group
                var groupedCorners = (from corner in allCorners
                                      group corner by new { corner.Priority.DirectionValue, corner.Priority.Distance } into grp
                                      orderby grp.Key.DirectionValue ascending, grp.Key.Distance
                                      select grp.ToList()).First();

                Random randCorner = new();
                return groupedCorners[randCorner.Next(0, groupedCorners.Count)];
            }
            else return null;
        }

        /// <summary>
        /// From a 1d view find the closest line to the bot. A corner is defined as 3 adjacent cells of myTerritory and the remaining cell of a different cell type
        /// </summary>
        /// <param name="botView"></param>
        /// <param name="clockwiseView"></param>
        /// <param name="myTerritory"></param>
        /// <returns></returns>
        public static CellFinderResult? FindLineCell(BotView botView, List<BotViewCell> clockwiseView, CellType myTerritory, BotAction currentDirection)
        {
            int xCount = botView.Cells.Count;
            int yCount = botView.Cells[0].Count;

            BotViewCell centerCell = botView.GetCenterCell();

            List<CellFinderResult> allLines = new();

            foreach (BotViewCell cell in clockwiseView)
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
                    break;
                }

                CellType rightType = botView.Cells[rightIndex.X][rightIndex.Y].CellType;
                CellType downType = botView.Cells[downIndex.X][downIndex.Y].CellType;
                CellType leftType = botView.Cells[leftIndex.X][leftIndex.Y].CellType;
                CellType upType = botView.Cells[upIndex.X][upIndex.Y].CellType;

                CellFinderPriority priority = new(centerCell.Location.DistanceTo(cell.Location), centerCell.Location.DirectionPriority(cell.Location, currentDirection));
                CellFinderResult cellFinder = new(cell, priority);

                //check that this cell has 3 adjacent cells that are myTerritory and the other 1 is not
                if (rightType == myTerritory && downType == myTerritory && leftType == myTerritory && upType != myTerritory)
                {
                    //up empty line
                    cellFinder.Directions = [new CellFinderDirection(LocationDirection.Right, RotationDirection.CounterClockwise)];

                    allLines.Add(cellFinder);
                }
                else if (rightType != myTerritory && downType == myTerritory && leftType == myTerritory && upType == myTerritory)
                {
                    //right empty line
                    cellFinder.Directions = [new CellFinderDirection(LocationDirection.Down, RotationDirection.Clockwise)];
                    allLines.Add(cellFinder);
                }
                else if (rightType == myTerritory && downType != myTerritory && leftType == myTerritory && upType == myTerritory)
                {
                    //down empty line
                    cellFinder.Directions = [new CellFinderDirection(LocationDirection.Left, RotationDirection.CounterClockwise)];
                    allLines.Add(cellFinder);
                }
                else if (rightType == myTerritory && downType == myTerritory && leftType != myTerritory && upType == myTerritory)
                {
                    //left empty line
                    cellFinder.Directions = [new CellFinderDirection(LocationDirection.Up, RotationDirection.Clockwise)];
                    allLines.Add(cellFinder);
                }
            }

            return allLines.OrderBy(x => x.Priority.DirectionValue).ThenBy(x => x.Priority.Distance).FirstOrDefault();
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
