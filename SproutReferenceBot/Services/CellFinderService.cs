using Microsoft.Extensions.Options;
using SproutReferenceBot.Enums;
using SproutReferenceBot.Extensions;
using SproutReferenceBot.Globals;
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
        /// Combine FindCornerCell and FindLineCell into 1 method
        /// </summary>
        /// <param name="botView"></param>
        /// <returns></returns>
        public static CellFinderResult? FindCaptureCell(BotView botView)
        {
            BotViewCell centerCell = botView.CenterCell();
            List<CellFinderResult> allResults = AllCornerCells(botView);
            allResults.AddRange(AllLineCells(botView));

            return SingleCellFinderResultFromList(allResults, centerCell);
        }

        private static CellFinderResult? SingleCellFinderResultFromList(List<CellFinderResult> allCells, BotViewCell centerCell)
        {
            //standing on cell / return that
            if (allCells.Any(x => x.Cell.Location == centerCell.Location))
            {
                return allCells.First(x => x.Cell.Location == centerCell.Location);
            }
            else if (allCells.Count > 0)
            {
                //group cells with the same priorities
                //then pick a random cell from that group
                var groupedCells = (from cell in allCells
                                    group cell by new { cell.Priority.DirectionValue, cell.Priority.Distance, CornerValue = cell.IsCorner ? 0 : 1, CaptureValue = cell.CanCapture ? 0 : 1 } into grp
                                    //Can capture or not, coarse distance order, then direction order, then Corner,, then fine distance order
                                    orderby grp.Key.CaptureValue ascending, (grp.Key.Distance / 2) ascending, grp.Key.DirectionValue ascending, grp.Key.CornerValue ascending, grp.Key.Distance ascending
                                    select grp.ToList()).First();

                Random randCell = new();
                return groupedCells[randCell.Next(0, groupedCells.Count)];
            }
            else return null;
        }

        private static List<CellFinderResult> AllCornerCells(BotView botView)
        {
            BotViewCell centerCell = botView.CenterCell();

            //find multiple corners and prioritise
            List<CellFinderResult> allCorners = [];

            foreach (BotViewCell cell in botView.ClockwiseView)
            {
                //ignore corners with weeds
                if (cell.HasWeed)
                {
                    continue;
                }

                List<BotViewCell> bufferList = cell.Location.CellBuffer();
                if (bufferList.Any(x => x.HasWeed) && !bufferList.Any(x => x.CellType == CellType.OutOfBounds))
                {
                    continue;
                }

                BotViewCell? rightCell = (cell.Location.Move(LocationDirection.Right)).ToBotViewCell();
                BotViewCell? downCell = (cell.Location.Move(LocationDirection.Down)).ToBotViewCell();
                BotViewCell? leftCell = (cell.Location.Move(LocationDirection.Left)).ToBotViewCell();
                BotViewCell? upCell = (cell.Location.Move(LocationDirection.Up)).ToBotViewCell();

                if (rightCell == null || downCell == null || leftCell == null || upCell == null)
                {
                    //the directions are no longer in bounds. There are no lines within view
                    break;
                }

                CellType[] validateSideTypes = [rightCell.CellType, downCell.CellType, leftCell.CellType, upCell.CellType];
                /*
                 * Not valid when:
                 * if 2 of the sides are out of bounds. Corner of map
                 * OR if there are not exactly 2 sides of BotServiceHelpers.MyTerritory
                 */
                if (validateSideTypes.Count(x => x == CellType.OutOfBounds) >= 2
                    || validateSideTypes.Count(x => x == BotServiceGlobals.MyTerritory) != 2)
                {
                    continue;
                }

                CellFinderPriority priority = new(centerCell.Location.DistanceTo(cell.Location), centerCell.Location.DirectionPriority(cell.Location, BotServiceGlobals.LastDirection));
                CellFinderResult cellFinder = new(cell, priority)
                {
                    CanCapture = true, //if it is added to the list, CanCapture is true
                    HasPriority = priority.DirectionValue < 0,
                    IsCorner = true,
                };

                bool canCaptureUp  = CanCaptureDirection(cell.Location, LocationDirection.Up);
                bool canCaptureRight = CanCaptureDirection(cell.Location, LocationDirection.Right);
                bool canCaptureDown = CanCaptureDirection(cell.Location, LocationDirection.Down);
                bool canCaptureLeft = CanCaptureDirection(cell.Location, LocationDirection.Left);

                //check that this cell has two adjacent cells that are BotServiceHelpers.MyTerritory and the other 2 are not
                if (rightCell.CellType == BotServiceGlobals.MyTerritory && downCell.CellType == BotServiceGlobals.MyTerritory && leftCell.CellType != BotServiceGlobals.MyTerritory && upCell.CellType != BotServiceGlobals.MyTerritory)
                {
                    //check each direction for safe movement
                    List<CellFinderDirection> tempDirections = [];
                    if (canCaptureUp && upCell.CellType != CellType.OutOfBounds)
                    {
                        tempDirections.Add(new(LocationDirection.Up, RotationDirection.Clockwise));
                    }
                    if (canCaptureLeft && leftCell.CellType != CellType.OutOfBounds)
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
                else if (rightCell.CellType != BotServiceGlobals.MyTerritory && downCell.CellType == BotServiceGlobals.MyTerritory && leftCell.CellType == BotServiceGlobals.MyTerritory && upCell.CellType != BotServiceGlobals.MyTerritory)
                {
                    List<CellFinderDirection> tempDirections = [];
                    if (canCaptureRight && rightCell.CellType != CellType.OutOfBounds)
                    {
                        tempDirections.Add(new(LocationDirection.Right, RotationDirection.Clockwise));
                    }
                    if (canCaptureUp && upCell.CellType != CellType.OutOfBounds)
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
                else if (rightCell.CellType != BotServiceGlobals.MyTerritory && downCell.CellType != BotServiceGlobals.MyTerritory && leftCell.CellType == BotServiceGlobals.MyTerritory && upCell.CellType == BotServiceGlobals.MyTerritory)
                {
                    List<CellFinderDirection> tempDirections = [];
                    if (canCaptureDown && downCell.CellType != CellType.OutOfBounds)
                    {
                        tempDirections.Add(new(LocationDirection.Down, RotationDirection.Clockwise));
                    }
                    if (canCaptureRight && rightCell.CellType != CellType.OutOfBounds)
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
                else if (rightCell.CellType == BotServiceGlobals.MyTerritory && downCell.CellType != BotServiceGlobals.MyTerritory && leftCell.CellType != BotServiceGlobals.MyTerritory && upCell.CellType == BotServiceGlobals.MyTerritory)
                {
                    List<CellFinderDirection> tempDirections = [];
                    if (canCaptureLeft && leftCell.CellType != CellType.OutOfBounds)
                    {
                        tempDirections.Add(new(LocationDirection.Left, RotationDirection.Clockwise));
                    }
                    if (canCaptureDown && downCell.CellType != CellType.OutOfBounds)
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

            return allCorners;
        }

        /// <summary>
        /// From a 1d view find the closest corner to the bot. A corner is defined as 2 adjacent cells of BotServiceHelpers.MyTerritory and the remaining 2 adjacent cells of another cell type)
        /// </summary>
        /// <param name="clockwiseView">1 dimensional list of the cell view. from GetClockwiseView</param>
        /// <param name="BotServiceHelpers.MyTerritory">The CellType that defines my bots territory</param>
        /// <returns>Null if no corner found in the botView</returns>
        public static CellFinderResult? FindCornerCell(BotView botView)
        {
            BotViewCell centerCell = botView.CenterCell();
            List<CellFinderResult> allCorners = AllCornerCells(botView);
            return SingleCellFinderResultFromList(allCorners, centerCell);
        }

        private static bool CanCaptureDirection(Location currentLocation, Location direction)
        {
            //add buffer to actual movement lengths (max capture length is 4)
            int movementLength = 6;

            List<MovementAction> movementActions = BotMovementService.MoveToDestinationBasic(currentLocation, currentLocation.Move(direction, movementLength));

            bool movementSafe = BotMovementService.AreMovementActionsSafe(movementActions);

            if (!movementSafe) return false;

            //avoid capturing into enemy territory near out of bounds. To avoid capturing spawns
            List<BotViewCell> actionCells = movementActions.ToBotViewCells();
            if (actionCells.Any(x => x.IsEnemyTerritory))
            {
                //look for out of bounds in many directions
                List<BotViewCell> clockwiseActionCells = BotMovementService.MoveToDestinationBasic(currentLocation, currentLocation.Move(direction.NextClockwiseDirection(), movementLength)).ToBotViewCells();
                List<BotViewCell> counterClockwiseActionCells = BotMovementService.MoveToDestinationBasic(currentLocation, currentLocation.Move(direction.NextCounterClockwiseDirection(), movementLength)).ToBotViewCells();

                if (actionCells.Any(x => x.CellType == CellType.OutOfBounds) 
                    || clockwiseActionCells.Any(x => x.CellType == CellType.OutOfBounds) 
                    || counterClockwiseActionCells.Any(x => x.CellType == CellType.OutOfBounds))
                {
                    return false;
                }
            }

            return true;

        }

        private static List<CellFinderResult> AllLineCells(BotView botView)
        {
            BotViewCell centerCell = botView.CenterCell();

            List<CellFinderResult> allLines = [];

            foreach (BotViewCell cell in botView.ClockwiseView)
            {
                if (cell.HasWeed)
                {
                    continue;
                }

                List<BotViewCell> bufferList = cell.Location.CellBuffer();
                //ignore lines with weeds
                if (bufferList.Any(x => x.HasWeed) && !bufferList.Any(x => x.CellType == CellType.OutOfBounds))
                {
                    continue;
                }

                BotViewCell? rightCell = (cell.Location.Move(LocationDirection.Right)).ToBotViewCell();
                BotViewCell? downCell = (cell.Location.Move(LocationDirection.Down)).ToBotViewCell();
                BotViewCell? leftCell = (cell.Location.Move(LocationDirection.Left)).ToBotViewCell();
                BotViewCell? upCell = (cell.Location.Move(LocationDirection.Up)).ToBotViewCell();

                if (rightCell == null || downCell == null || leftCell == null || upCell == null)
                {
                    //the directions are no longer in bounds. There are no lines within view
                    break;
                }

                CellFinderPriority priority = new(centerCell.Location.DistanceTo(cell.Location), centerCell.Location.DirectionPriority(cell.Location, BotServiceGlobals.LastDirection));
                CellFinderResult cellFinder = new(cell, priority)
                {
                    HasPriority = priority.DirectionValue < 0
                };

                //check that this cell has 3 adjacent cells that are BotServiceHelpers.MyTerritory and the other 1 is not
                if (rightCell.CellType == BotServiceGlobals.MyTerritory && downCell.CellType == BotServiceGlobals.MyTerritory && leftCell.CellType == BotServiceGlobals.MyTerritory && upCell.CellType != BotServiceGlobals.MyTerritory)
                {
                    //up empty line
                    if (upCell.CellType != CellType.OutOfBounds)
                    {
                        //check which direction to go in
                        if (CountMyTerritory(botView, new CellFinderDirection(LocationDirection.Up, RotationDirection.Clockwise)) <= CountMyTerritory(botView, new CellFinderDirection(LocationDirection.Up, RotationDirection.CounterClockwise)))
                        {
                            cellFinder.Directions = [new CellFinderDirection(LocationDirection.Up, RotationDirection.Clockwise)];
                        }
                        else
                        {
                            cellFinder.Directions = [new CellFinderDirection(LocationDirection.Up, RotationDirection.CounterClockwise)];
                        }

                        //check that the initial capture path is safe 
                        cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Up);
                    }
                    else
                    {
                        cellFinder.Directions = [new CellFinderDirection(LocationDirection.Right, RotationDirection.CounterClockwise)];
                    }

                    allLines.Add(cellFinder);
                }
                else if (rightCell.CellType != BotServiceGlobals.MyTerritory && downCell.CellType == BotServiceGlobals.MyTerritory && leftCell.CellType == BotServiceGlobals.MyTerritory && upCell.CellType == BotServiceGlobals.MyTerritory)
                {
                    //right empty line
                    if (rightCell.CellType != CellType.OutOfBounds)
                    {
                        if (CountMyTerritory(botView, new CellFinderDirection(LocationDirection.Right, RotationDirection.Clockwise)) <= CountMyTerritory(botView, new CellFinderDirection(LocationDirection.Right, RotationDirection.CounterClockwise)))
                        {
                            cellFinder.Directions = [new CellFinderDirection(LocationDirection.Right, RotationDirection.Clockwise)];
                        }
                        else
                        {
                            cellFinder.Directions = [new CellFinderDirection(LocationDirection.Right, RotationDirection.CounterClockwise)];
                        }

                        //check that the initial capture path is safe 
                        cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Right);
                    }
                    else
                    {
                        cellFinder.Directions = [new CellFinderDirection(LocationDirection.Down, RotationDirection.Clockwise)];
                    }

                    allLines.Add(cellFinder);
                }
                else if (rightCell.CellType == BotServiceGlobals.MyTerritory && downCell.CellType != BotServiceGlobals.MyTerritory && leftCell.CellType == BotServiceGlobals.MyTerritory && upCell.CellType == BotServiceGlobals.MyTerritory)
                {
                    //down empty line
                    if (downCell.CellType != CellType.OutOfBounds)
                    {
                        if (CountMyTerritory(botView, new CellFinderDirection(LocationDirection.Down, RotationDirection.Clockwise)) <= CountMyTerritory(botView, new CellFinderDirection(LocationDirection.Down, RotationDirection.CounterClockwise)))
                        {
                            cellFinder.Directions = [new CellFinderDirection(LocationDirection.Down, RotationDirection.Clockwise)];
                        }
                        else
                        {
                            cellFinder.Directions = [new CellFinderDirection(LocationDirection.Down, RotationDirection.CounterClockwise)];
                        }

                        //check that the initial capture path is safe 
                        cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Down);

                    }
                    else
                    {
                        cellFinder.Directions = [new CellFinderDirection(LocationDirection.Left, RotationDirection.CounterClockwise)];
                    }

                    allLines.Add(cellFinder);
                }
                else if (rightCell.CellType == BotServiceGlobals.MyTerritory && downCell.CellType == BotServiceGlobals.MyTerritory && leftCell.CellType != BotServiceGlobals.MyTerritory && upCell.CellType == BotServiceGlobals.MyTerritory)
                {
                    //left empty line
                    if (leftCell.CellType != CellType.OutOfBounds)
                    {
                        if (CountMyTerritory(botView, new CellFinderDirection(LocationDirection.Left, RotationDirection.Clockwise)) <= CountMyTerritory(botView, new CellFinderDirection(LocationDirection.Left, RotationDirection.CounterClockwise)))
                        {
                            cellFinder.Directions = [new CellFinderDirection(LocationDirection.Left, RotationDirection.Clockwise)];
                        }
                        else
                        {
                            cellFinder.Directions = [new CellFinderDirection(LocationDirection.Left, RotationDirection.CounterClockwise)];
                        }

                        //check that the initial capture path is safe 
                        cellFinder.CanCapture = CanCaptureDirection(cell.Location, LocationDirection.Left);
                    }
                    else
                    {
                        cellFinder.Directions = [new CellFinderDirection(LocationDirection.Up, RotationDirection.Clockwise)];
                    }

                    allLines.Add(cellFinder);
                }
            }

            return allLines;
        }

        /// <summary>
        /// From a 1d view find the closest line to the bot. A corner is defined as 3 adjacent cells of BotServiceHelpers.MyTerritory and the remaining cell of a different cell type
        /// </summary>
        /// <param name="botView"></param>
        /// <param name="clockwiseView"></param>
        /// <param name="BotServiceHelpers.MyTerritory"></param>
        /// <returns></returns>
        public static CellFinderResult? FindLineCell(BotView botView)
        {
            BotViewCell centerCell = botView.CenterCell();
            List<CellFinderResult> allLines = AllLineCells(botView);
            return SingleCellFinderResultFromList(allLines, centerCell);
        }

        /// <summary>
        /// Count the number of cells of myTerritory in two directions (e.g. all cells down and right of the center cell)
        /// </summary>
        public static int CountMyTerritory(BotView botView, CellFinderDirection direction)
        {
            BotViewCell centerCell = botView.CenterCell();
            //get all cells in two directions
            Location direction1 = direction.Direction;
            Location direction2;
            if (direction.Rotation == RotationDirection.Clockwise)
            {
                direction2 = direction.Direction.NextClockwiseDirection();
            }
            else
            {
                direction2 = direction.Direction.NextCounterClockwiseDirection();
            }

            Location currentLocation = centerCell.Location.Move(direction1);
            Location previousLocation = centerCell.Location;

            int myTerritoryCount = 0;
            //move in 1 direction slowly
            while ((currentLocation).ToBotViewCell() != null)
            {
                //move in all direction2 for a single direction1
                //e.g. do all x cells for this y
                while ((currentLocation).ToBotViewCell() != null)
                {
                    currentLocation = currentLocation.Move(direction2);

                    if ((currentLocation).ToBotViewCell()?.CellType == BotServiceGlobals.MyTerritory)
                    {
                        myTerritoryCount++;
                    }
                }

                //reset to base direction2 and move up direction1
                currentLocation = previousLocation.Move(direction1);
                previousLocation = currentLocation;
            }

            return myTerritoryCount;
        }
    }
}
