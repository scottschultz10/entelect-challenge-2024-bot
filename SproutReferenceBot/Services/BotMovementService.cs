using SproutReferenceBot.Enums;
using SproutReferenceBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Services
{
    public static class BotMovementService
    {

        public static List<MovementAction> MoveToDestination(Location current, Location destination, BotView botView, BotAction currentDirection, RotationDirection? captureRotation, Location movementOffset)
        {
            //make the basic movement first
            Location difference = current.Difference(destination.Move(movementOffset));
            Location currentLocation = current;
            BotAction lastDirection = currentDirection;

            List<MovementAction> movementList = new();

            while (difference != LocationDirection.NONE)
            {
                bool hasMoved = false;

                //y first
                if (difference.Y > 0 && lastDirection != BotAction.Down)
                {
                    difference = difference.Move(LocationDirection.Up);
                    currentLocation = currentLocation.Move(LocationDirection.Up);

                    movementList.Add(new(BotAction.Up, currentLocation));
                    lastDirection = BotAction.Up;
                    hasMoved = true;
                }
                else if (difference.Y < 0 && lastDirection != BotAction.Up)
                {
                    difference = difference.Move(LocationDirection.Down);
                    currentLocation = currentLocation.Move(LocationDirection.Down);

                    movementList.Add(new(BotAction.Down, currentLocation));
                    lastDirection = BotAction.Down;
                    hasMoved = true;
                }
                //else difference == 0 / do nothing - at destination X

                //then x
                if (difference.X > 0 && lastDirection != BotAction.Right)
                {
                    difference = difference.Move(LocationDirection.Left);
                    currentLocation = currentLocation.Move(LocationDirection.Left);

                    movementList.Add(new(BotAction.Left, currentLocation));
                    lastDirection = BotAction.Left;
                    hasMoved = true;
                }
                else if (difference.X < 0 && lastDirection != BotAction.Left)
                {
                    difference = difference.Move(LocationDirection.Right);
                    currentLocation = currentLocation.Move(LocationDirection.Right);

                    movementList.Add(new(BotAction.Right, currentLocation));
                    lastDirection = BotAction.Right;
                    hasMoved = true;
                }
                //else difference == 0 / do nothing - at destination Y

                if (!hasMoved && difference != LocationDirection.NONE)
                {
                    Location fixDirection;
                    if (captureRotation == RotationDirection.CounterClockwise)
                    {
                        fixDirection = lastDirection.ToLocationDirection().NextCounterClockwiseDirection();
                    }
                    else
                    {
                        fixDirection = lastDirection.ToLocationDirection().NextClockwiseDirection();
                    }

                    difference = difference.Move(fixDirection);
                    currentLocation = currentLocation.Move(fixDirection);

                    movementList.Add(new(fixDirection.ToBotAction(), currentLocation));
                    lastDirection = fixDirection.ToBotAction();
                }
            }

            //then check hazards on trail / and avoid
            if (movementList.Count > 0 && !AreMovementActionsSafe(movementList, botView))
            {
                //go the opposite direction of the rotation
                //offset by the most common direction
                Location offset;
                BotAction commonDirection = (from move in movementList
                                             group move by move.Action into grp
                                             select new
                                             {
                                                 grp.Key,
                                                 Count = grp.Count(),
                                             }).OrderByDescending(x => x.Count).First().Key;

                if (captureRotation == RotationDirection.Clockwise)
                {
                    offset = movementOffset.Move(commonDirection.ToLocationDirection().NextCounterClockwiseDirection());
                }
                else
                {
                    offset = movementOffset.Move(commonDirection.ToLocationDirection().NextClockwiseDirection());
                }

                Console.Write($"Offsetting the movement: {offset}");

                return MoveToDestination(current, destination, botView, currentDirection, captureRotation, offset);
            }
            else
            {
                return movementList;
            }
        }

        public static List<MovementQueueItem> CaptureTerritory(CellFinderResult cellFinder, BotView botView, CellType myTerritory)
        {
            List<List<MovementQueueItem>> allQueues = new();

            foreach (CellFinderDirection direction in cellFinder.Directions)
            {
                allQueues.Add(CaptureTerritoryQueue(botView, cellFinder.Cell.Location, direction, myTerritory));
            }

            int maxIndex = 0;
            int maxDistance = 0;
            for (int i = 0; i < allQueues.Count; i++)
            {
                Console.WriteLine($"action queue count: {allQueues[i].Count}");

                int totalDistance = 0;
                if (allQueues.Count > 1)
                {
                    Location currentLocation = allQueues[i][0].Destination;

                    for (int loc = 1; loc < allQueues[i].Count; loc++)
                    {
                        totalDistance += currentLocation.DistanceTo(allQueues[i][loc].Destination);
                        currentLocation = allQueues[i][loc].Destination;
                    }
                }

                if (totalDistance > maxDistance)
                {
                    maxIndex = i;
                    maxDistance = totalDistance;
                }
            }

            return allQueues[maxIndex];
        }

        private static List<MovementQueueItem> CaptureTerritoryQueue(BotView botView, Location cell, CellFinderDirection direction, CellType myTerritory)
        {
            List<MovementQueueItem> destinationList = new();

            Location currentDirection = direction.Direction;
            Location currentLocation = cell;

            int directionMax = 4;

            for (int directionChangeCount = 0; directionChangeCount < 4; directionChangeCount++)
            {
                //increase the max to avoid colliding with ourselves
                if (directionChangeCount == 2)
                {
                    directionMax++;
                }

                List<(Location Location, BotViewCell? Cell)?> cells = new();
                for (int i = 1; i <= directionMax; i++)
                {
                    Location cellLocation = currentLocation.Move(currentDirection, i);
                    cells.Add((cellLocation, botView.GetCellByLocation(cellLocation)));
                }

                //find a valid destination
                //stay in bounds
                Location tempLocation;

                if (cells.Any(x => x?.Cell?.CellType == CellType.OutOfBounds))
                {
                    tempLocation = cells.LastOrDefault(x => x?.Cell?.CellType != CellType.OutOfBounds)?.Location ?? currentLocation;
                }
                else if (cells.Any(x => x?.Cell?.CellType == myTerritory))
                {
                    tempLocation = cells.LastOrDefault(x => x?.Cell?.CellType == myTerritory)?.Location ?? currentLocation;
                }
                else
                {
                    tempLocation = cells.LastOrDefault()?.Location ?? currentLocation;
                }

                if (botView.GetCellByLocation(tempLocation)?.CellType != CellType.OutOfBounds && tempLocation != currentDirection)
                {
                    destinationList.Add(new(tempLocation, new(currentDirection, direction.Rotation)));
                    currentLocation = tempLocation;

                    //we have hit myTerritory, stop capturing
                    if (botView.GetCellByLocation(currentLocation)?.CellType == myTerritory)
                    {
                        break;
                    }
                }

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

            return destinationList;
        }

        public static List<MovementQueueItem> MoveAlongLine(CellFinderResult cellFinder, BotView botView, CellType myTerritory)
        {
            List<MovementQueueItem> movementList = new();

            BotViewCell? currentCell = cellFinder.Cell;
            Location currentLocation = cellFinder.Cell.Location;

            int directionMagnitude = 4;
            Location tempLocation = currentLocation.Move(cellFinder.Directions.First().Direction, directionMagnitude);

            //stay in bounds
            while (botView.GetCellByLocation(tempLocation)?.CellType != myTerritory && directionMagnitude > 1)
            {
                directionMagnitude--;
                tempLocation = currentLocation.Move(cellFinder.Directions.First().Direction, directionMagnitude);
            }

            return new() { new(tempLocation, cellFinder.Directions.First()) };
        }

        public static bool CanMove(this BotAction currentDirection, BotAction newDirection)
        {
            if (currentDirection == BotAction.Up && newDirection == BotAction.Down
                || currentDirection == BotAction.Down && newDirection == BotAction.Up
                || currentDirection == BotAction.Left && newDirection == BotAction.Right
                || currentDirection == BotAction.Right && newDirection == BotAction.Left)
            {
                return false;
            }
            else return true;

        }

        public static bool AreMovementActionsSafe(this List<MovementAction> actions, BotView botView)
        {
            return !actions.Any(x => x.GetBotViewCell(botView)?.HasWeed == true);
        }

    }
}
