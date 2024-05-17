using SproutReferenceBot.Enums;
using SproutReferenceBot.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Services
{

    public static class CellFinderService
    {
        /// <summary>
        /// Build a 1 dimensional list of cellviews starting from the bot and spiralling outwards
        /// </summary>
        /// <returns>A 1 dimensional list of cellviews that gradually get further from the bot as the list continues</returns>
        public static List<BotViewCell> GetClockwiseView(BotView botView)
        {
            if (botView.Cells.Count == 0) return new();

            BotViewCell centerCell = botView.CenterCell();

            List<BotViewCell> clockwiseView = new() { centerCell };

            BotViewCell? currentCell = botView.CellByLocation(centerCell.Location.Move(LocationDirection.Right));
            Location currentDirection = LocationDirection.Down;
            int directionMax = 1;
            int directionCount = 0;

            while (currentCell != null)
            {
                clockwiseView.Add(currentCell);

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
                currentCell = botView.CellByLocation(currentCell.Location.Move(currentDirection));
            }

            return clockwiseView;
        }

        /// <summary>
        /// From a 1d view find the closest corner to the bot. A corner is defined as 2 adjacent cells of BotServiceHelpers.MyTerritory and the remaining 2 adjacent cells of another cell type)
        /// </summary>
        /// <param name="clockwiseView">1 dimensional list of the cell view. from GetClockwiseView</param>
        /// <param name="BotServiceHelpers.MyTerritory">The CellType that defines my bots territory</param>
        /// <returns>Null if no corner found in the botView</returns>
        public static CellFinderResult? FindCornerCell(BotView botView, List<BotViewCell> clockwiseView)
        {
            BotViewCell centerCell = botView.CenterCell();

            //find multiple corners and prioritise
            List<CellFinderResult> allCorners = new();

            foreach (BotViewCell cell in clockwiseView)
            {
                //ignore corners with weeds
                if (cell.HasWeed)
                {
                    continue;
                }

                List<BotViewCell> bufferList = botView.CellBufferByLocation(cell.Location);
                if (bufferList.Any(x => x.HasWeed) && !bufferList.Any(x => x.CellType == CellType.OutOfBounds))
                {
                    continue;
                }

                BotViewCell? rightCell = botView.CellByLocation(cell.Location.Move(LocationDirection.Right));
                BotViewCell? downCell = botView.CellByLocation(cell.Location.Move(LocationDirection.Down));
                BotViewCell? leftCell = botView.CellByLocation(cell.Location.Move(LocationDirection.Left));
                BotViewCell? upCell = botView.CellByLocation(cell.Location.Move(LocationDirection.Up));

                if (rightCell == null || downCell == null || leftCell == null || upCell == null)
                {
                    //the directions are no longer in bounds. There are no lines within view
                    break;
                }

                CellType[] validateSideTypes = { rightCell.CellType, downCell.CellType, leftCell.CellType, upCell.CellType };
                /*
                 * Not valid when:
                 * if 2 of the sides are out of bounds. Corner of map
                 * OR if there are not exactly 2 sides of BotServiceHelpers.MyTerritory
                 */
                if (validateSideTypes.Count(x => x == CellType.OutOfBounds) >= 2
                    || validateSideTypes.Count(x => x == BotServiceHelpers.MyTerritory) != 2)
                {
                    continue;
                }

                CellFinderPriority priority = new(centerCell.Location.DistanceTo(cell.Location), centerCell.Location.DirectionPriority(cell.Location, BotServiceHelpers.LastDirection));
                CellFinderResult cellFinder = new(cell, priority);
                cellFinder.HasPriority = priority.DirectionValue < 0;

                bool canCaptureUp = cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Up, botView);
                bool canCaptureRight = cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Right, botView);
                bool canCaptureDown = cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Down, botView);
                bool canCaptureLeft = CanCaptureDirection(cell.Location, LocationDirection.Left, botView);

                //check that this cell has two adjacent cells that are BotServiceHelpers.MyTerritory and the other 2 are not
                if (rightCell.CellType == BotServiceHelpers.MyTerritory && downCell.CellType == BotServiceHelpers.MyTerritory && leftCell.CellType != BotServiceHelpers.MyTerritory && upCell.CellType != BotServiceHelpers.MyTerritory)
                {
                    //check each direction for safe movement
                    List<CellFinderDirection> tempDirections = new();
                    if (canCaptureUp)
                    {
                        tempDirections.Add(new(LocationDirection.Up, RotationDirection.Clockwise));
                    }
                    if (canCaptureLeft)
                    {
                        tempDirections.Add(new(LocationDirection.Left, RotationDirection.CounterClockwise));
                    }

                    if (tempDirections.Count > 0)
                    {
                        cellFinder.Directions = tempDirections;

                        //right-down corner
                        allCorners.Add(cellFinder);
                    }
                }
                else if (rightCell.CellType != BotServiceHelpers.MyTerritory && downCell.CellType == BotServiceHelpers.MyTerritory && leftCell.CellType == BotServiceHelpers.MyTerritory && upCell.CellType != BotServiceHelpers.MyTerritory)
                {
                    List<CellFinderDirection> tempDirections = new();
                    if (canCaptureRight)
                    {
                        tempDirections.Add(new(LocationDirection.Right, RotationDirection.Clockwise));
                    }
                    if (canCaptureUp)
                    {
                        tempDirections.Add(new(LocationDirection.Up, RotationDirection.CounterClockwise));
                    }

                    if (tempDirections.Count > 0)
                    {
                        cellFinder.Directions = tempDirections;

                        //down-left corner
                        allCorners.Add(cellFinder);
                    }
                }
                else if (rightCell.CellType != BotServiceHelpers.MyTerritory && downCell.CellType != BotServiceHelpers.MyTerritory && leftCell.CellType == BotServiceHelpers.MyTerritory && upCell.CellType == BotServiceHelpers.MyTerritory)
                {
                    List<CellFinderDirection> tempDirections = new();
                    if (canCaptureDown)
                    {
                        tempDirections.Add(new(LocationDirection.Down, RotationDirection.Clockwise));
                    }
                    if (canCaptureRight)
                    {
                        tempDirections.Add(new(LocationDirection.Right, RotationDirection.CounterClockwise));
                    }

                    if (tempDirections.Count > 0)
                    {
                        cellFinder.Directions = tempDirections;

                        //left-up corner
                        allCorners.Add(cellFinder);
                    }
                }
                else if (rightCell.CellType == BotServiceHelpers.MyTerritory && downCell.CellType != BotServiceHelpers.MyTerritory && leftCell.CellType != BotServiceHelpers.MyTerritory && upCell.CellType == BotServiceHelpers.MyTerritory)
                {
                    List<CellFinderDirection> tempDirections = new();
                    if (canCaptureLeft)
                    {
                        tempDirections.Add(new(LocationDirection.Left, RotationDirection.Clockwise));
                    }
                    if (canCaptureDown)
                    {
                        tempDirections.Add(new(LocationDirection.Down, RotationDirection.CounterClockwise));
                    }
        
                    if (tempDirections.Count > 0)
                    {
                        cellFinder.Directions = tempDirections;

                        //up-right corner
                        allCorners.Add(cellFinder);
                    }
                }
            }

            //Console.WriteLine($"All Corners {centerCell.Location} = {string.Join(", ", allCorners.Select(x => x.Cell.Location))}");

            //standing on corner cell / return that
            if (allCorners.Any(x => x.Cell.Location == centerCell.Location))
            {
                return allCorners.First(x => x.Cell.Location == centerCell.Location);
            }
            else if (allCorners.Count > 0)
            {
                //group corners with the same priorities
                //then pick a random corner from that group
                var groupedCorners = (from corner in allCorners
                                      group corner by new { corner.Priority.DirectionValue, corner.Priority.Distance } into grp
                                      orderby (grp.Key.Distance / 3) ascending, grp.Key.DirectionValue ascending
                                      select grp.ToList()).First();

                Random randCorner = new();
                return groupedCorners[randCorner.Next(0, groupedCorners.Count)];
            }
            else return null;
        }

        private static bool CanCaptureDirection(Location currentLocation, Location direction, BotView botView)
        {
            return BotMovementService.AreMovementActionsSafe(BotMovementService.MoveToDestinationBasic(currentLocation, currentLocation.Move(direction, 4)), botView);
        }

        /// <summary>
        /// From a 1d view find the closest line to the bot. A corner is defined as 3 adjacent cells of BotServiceHelpers.MyTerritory and the remaining cell of a different cell type
        /// </summary>
        /// <param name="botView"></param>
        /// <param name="clockwiseView"></param>
        /// <param name="BotServiceHelpers.MyTerritory"></param>
        /// <returns></returns>
        public static CellFinderResult? FindLineCell(BotView botView, List<BotViewCell> clockwiseView)
        {
            BotViewCell centerCell = botView.CenterCell();

            List<CellFinderResult> allLines = new();

            foreach (BotViewCell cell in clockwiseView)
            {
                if (cell.HasWeed)
                {
                    continue;
                }

                List<BotViewCell> bufferList = botView.CellBufferByLocation(cell.Location);
                //ignore lines with weeds
                if (bufferList.Any(x => x.HasWeed) && !bufferList.Any(x => x.CellType == CellType.OutOfBounds))
                {
                    continue;
                }

                BotViewCell? rightCell = botView.CellByLocation(cell.Location.Move(LocationDirection.Right));
                BotViewCell? downCell = botView.CellByLocation(cell.Location.Move(LocationDirection.Down));
                BotViewCell? leftCell = botView.CellByLocation(cell.Location.Move(LocationDirection.Left));
                BotViewCell? upCell = botView.CellByLocation(cell.Location.Move(LocationDirection.Up));

                if (rightCell == null || downCell == null || leftCell == null || upCell == null)
                {
                    //the directions are no longer in bounds. There are no lines within view
                    break;
                }

                CellFinderPriority priority = new(centerCell.Location.DistanceTo(cell.Location), centerCell.Location.DirectionPriority(cell.Location, BotServiceHelpers.LastDirection));
                CellFinderResult cellFinder = new(cell, priority);
                cellFinder.HasPriority = priority.DirectionValue < 0;

                //check that this cell has 3 adjacent cells that are BotServiceHelpers.MyTerritory and the other 1 is not
                if (rightCell.CellType == BotServiceHelpers.MyTerritory && downCell.CellType == BotServiceHelpers.MyTerritory && leftCell.CellType == BotServiceHelpers.MyTerritory && upCell.CellType != BotServiceHelpers.MyTerritory)
                {
                    //up empty line
                    if (upCell.CellType != CellType.OutOfBounds)
                    {
                        cellFinder.Directions = new()
                        {
                            new CellFinderDirection(LocationDirection.Up, RotationDirection.Clockwise),
                            new CellFinderDirection(LocationDirection.Up, RotationDirection.CounterClockwise),
                        };

                        //check that the initial capture path is safe 
                        cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Up, botView);
                    }
                    else
                    {
                        cellFinder.Directions = [new CellFinderDirection(LocationDirection.Right, RotationDirection.CounterClockwise)];
                    }

                    allLines.Add(cellFinder);
                }
                else if (rightCell.CellType != BotServiceHelpers.MyTerritory && downCell.CellType == BotServiceHelpers.MyTerritory && leftCell.CellType == BotServiceHelpers.MyTerritory && upCell.CellType == BotServiceHelpers.MyTerritory)
                {
                    //right empty line
                    if (rightCell.CellType != CellType.OutOfBounds)
                    {
                        cellFinder.Directions = new()
                        {
                            new CellFinderDirection(LocationDirection.Right, RotationDirection.Clockwise),
                            new CellFinderDirection(LocationDirection.Right, RotationDirection.CounterClockwise),
                        };
                        //check that the initial capture path is safe 
                        cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Right, botView);

                    }
                    else
                    {
                        cellFinder.Directions = [new CellFinderDirection(LocationDirection.Down, RotationDirection.Clockwise)];
                    }

                    allLines.Add(cellFinder);
                }
                else if (rightCell.CellType == BotServiceHelpers.MyTerritory && downCell.CellType != BotServiceHelpers.MyTerritory && leftCell.CellType == BotServiceHelpers.MyTerritory && upCell.CellType == BotServiceHelpers.MyTerritory)
                {
                    //down empty line
                    if (downCell.CellType != CellType.OutOfBounds)
                    {
                        cellFinder.Directions = new()
                        {
                            new CellFinderDirection(LocationDirection.Down, RotationDirection.Clockwise),
                            new CellFinderDirection(LocationDirection.Down, RotationDirection.CounterClockwise),
                        };
                        //check that the initial capture path is safe 
                        cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Down, botView);

                    }
                    else
                    {
                        cellFinder.Directions = [new CellFinderDirection(LocationDirection.Left, RotationDirection.CounterClockwise)];
                    }

                    allLines.Add(cellFinder);
                }
                else if (rightCell.CellType == BotServiceHelpers.MyTerritory && downCell.CellType == BotServiceHelpers.MyTerritory && leftCell.CellType != BotServiceHelpers.MyTerritory && upCell.CellType == BotServiceHelpers.MyTerritory)
                {
                    //left empty line
                    if (leftCell.CellType != CellType.OutOfBounds)
                    {
                        cellFinder.Directions = new()
                        {
                            new CellFinderDirection(LocationDirection.Left, RotationDirection.Clockwise),
                            new CellFinderDirection(LocationDirection.Left, RotationDirection.CounterClockwise),
                        };
                        //check that the initial capture path is safe 
                        cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Left, botView);

                    }
                    else
                    {
                        cellFinder.Directions = [new CellFinderDirection(LocationDirection.Up, RotationDirection.Clockwise)];
                    }

                    allLines.Add(cellFinder);
                }
            }

            if (allLines.Any(x => x.Cell.Location == centerCell.Location))
            {
                return allLines.First(x => x.Cell.Location == centerCell.Location);
            }
            else if (allLines.Count > 0)
            {
                //group lines with the same priorities
                //then pick a random line from that group
                var groupedLines = (from line in allLines
                                    group line by new { line.Priority.DirectionValue, line.Priority.Distance } into grp
                                    orderby (grp.Key.DirectionValue / 3) ascending, grp.Key.Distance ascending
                                    select grp.ToList()).First();

                Random randCorner = new();
                return groupedLines[randCorner.Next(0, groupedLines.Count)];
            }
            else return null;
        }

    }
}
