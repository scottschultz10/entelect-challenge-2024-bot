using SproutReferenceBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Models
{
    public static class BotMovement
    {
        private class MovementItem(BotAction action, BotViewCell? cell)
        {
            public BotAction Action = action;
            public BotViewCell? Cell = cell;

            public override string ToString()
            {
                return $"{Action}, Cell: {Cell?.Location}, {Cell?.CellType}";
            }
        }

        public static Queue<BotAction> MoveToDestination(BotViewCell current, BotViewCell destination, BotView botView)
        {
            //make the basic movement first
            Location difference = current.Location.Difference(destination.Location);

            List<MovementItem> movementList = new();

            while (difference != LocationDirection.NONE)
            {
                //x first
                if (difference.X > 0)
                {
                    difference = difference.Move(LocationDirection.Up);
                    movementList.Add(new(BotAction.Up, botView.GetCellByLocation(difference)));
                }
                else if (difference.X < 0)
                {
                    difference = difference.Move(LocationDirection.Down);
                    movementList.Add(new(BotAction.Down, botView.GetCellByLocation(difference)));
                }
                //else difference == 0 / do nothing - at destination X

                //then y
                if (difference.Y > 0)
                {
                    difference = difference.Move(LocationDirection.Left);
                    movementList.Add(new(BotAction.Left, botView.GetCellByLocation(difference)));
                }
                else if (difference.Y < 0)
                {
                    difference = difference.Move(LocationDirection.Right);
                    movementList.Add(new(BotAction.Right, botView.GetCellByLocation(difference)));
                }
                //else difference == 0 / do nothing - at destination Y
            }

            //then check hazards on trail / and avoid

            return new(movementList.Select(x => x.Action));
        }

        public static Queue<BotAction> CaptureTerritory(CellFinderResult cellFinder, BotView botView, CellType myTerritory)
        {
            List<Queue<BotAction>> allQueues = new();

            foreach(CellFinderDirection direction in cellFinder.Directions)
            {
                allQueues.Add(CaptureTerritoryQueue(botView, cellFinder.Cell, direction, myTerritory));
            }

            int maxIndex = 0;
            int maxCount = 0;
            for (int i = 0; i < allQueues.Count; i++)
            {
                if (allQueues[i].Count > maxCount)
                {
                    maxIndex = i;
                    maxCount = allQueues[i].Count;
                }
            }

            return allQueues[maxIndex];
        }

        private static Queue<BotAction> CaptureTerritoryQueue(BotView botView, BotViewCell cell, CellFinderDirection direction, CellType myTerritory)
        {
            List<MovementItem> captureList = new();

            BotViewCell? currentCell = cell;
            Location currentDirection = direction.Direction;
            Location currentLocation = cell.Location;
            int directionMax = 4;
            int directionCount = 0;
            bool directionHasChanged = false;

            do
            {
                if (directionCount >= directionMax)
                {
                    //reset the count
                    directionCount = 0;
                    directionMax++;

                    directionHasChanged = true;

                    //change the direction
                    if (direction.Rotation == RotationDirection.Clockwise)
                    {
                        currentDirection = currentDirection.NextClockwiseDirection();
                    }
                    else
                    {
                        currentDirection = currentDirection.NextCounterClockwiseDirection();
                    }
                }

                directionCount++;

                //See what the bot is moving into before commiting to a move
                Location tempLocation = currentLocation.Move(currentDirection);
                BotViewCell? tempCell = botView.GetCellByLocation(tempLocation);

                if (tempCell?.CellType == CellType.OutOfBounds)
                {
                    //can't go to out of bounds so force a change in direction
                    directionMax--; //prevent unnecessary increments
                    directionCount = directionMax;
                }
                else
                {
                    currentLocation = tempLocation;
                    currentCell = tempCell;
                    captureList.Add(new(currentDirection.ToBotAction(), currentCell));
                }
            }
            while (captureList.LastOrDefault()?.Cell?.CellType != myTerritory || !directionHasChanged);

            return new(captureList.Select(x => x.Action));
        }

        public static Queue<BotAction> MoveAlongLine(CellFinderResult cellFinder, BotView botView, CellType myTerritory)
        {
            List<MovementItem> movementList = new();

            BotViewCell? currentCell = cellFinder.Cell;
            Location currentLocation = cellFinder.Cell.Location;

            while(currentCell != null && currentCell.CellType == myTerritory)
            {
                //See what the bot is moving into before commiting to a move
                Location tempLocation = currentLocation.Move(cellFinder.Directions.First().Direction);
                BotViewCell? tempCell = botView.GetCellByLocation(tempLocation);

                if (tempCell?.CellType == CellType.OutOfBounds)
                {
                    break;
                }
                else
                {
                    currentLocation = tempLocation;
                    currentCell = tempCell;
                    movementList.Add(new(cellFinder.Directions.First().Direction.ToBotAction(), currentCell));
                }
            }

            return new(movementList.Select(x => x.Action));
        }

        public static bool CanMove(this BotAction currentDirection, BotAction newDirection)
        {
            if ((currentDirection == BotAction.Up && newDirection == BotAction.Down)
                || (currentDirection == BotAction.Down && newDirection == BotAction.Up)
                || (currentDirection == BotAction.Left && newDirection == BotAction.Right)
                || (currentDirection == BotAction.Right && newDirection == BotAction.Left))
            {
                return false;
            }
            else return true;

        }
    }
}
