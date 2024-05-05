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
       
        public static List<MovementAction> MoveToDestination(Location current, Location destination, BotView botView)
        {
            //make the basic movement first
            Location difference = current.Difference(destination);
            Location currentLocation = current;

            Console.WriteLine($"Moving to Destination {destination} ->  Difference : {difference}");

            List <MovementAction> movementList = new();

            while (difference != LocationDirection.NONE)
            {
                //x first
                if (difference.X > 0)
                {
                    difference = difference.Move(LocationDirection.Up);
                    currentLocation = currentLocation.Move(LocationDirection.Up);

                    movementList.Add(new(BotAction.Up, currentLocation));
                }
                else if (difference.X < 0)
                {
                    difference = difference.Move(LocationDirection.Down);
                    currentLocation = currentLocation.Move(LocationDirection.Down);

                    movementList.Add(new(BotAction.Down, currentLocation));
                }
                //else difference == 0 / do nothing - at destination X

                //then y
                if (difference.Y > 0)
                {
                    difference = difference.Move(LocationDirection.Left);
                    currentLocation = currentLocation.Move(LocationDirection.Left);

                    movementList.Add(new(BotAction.Left, currentLocation));
                }
                else if (difference.Y < 0)
                {
                    difference = difference.Move(LocationDirection.Right);
                    currentLocation = currentLocation.Move(LocationDirection.Right);

                    movementList.Add(new(BotAction.Right, currentLocation));
                }
                //else difference == 0 / do nothing - at destination Y
            }

            //then check hazards on trail / and avoid

            Console.WriteLine($"... {string.Join("; ", movementList)}");

            return movementList;
        }

        public static List<MovementQueueItem> CaptureTerritory(CellFinderResult cellFinder, BotView botView)
        {
            List<List<MovementQueueItem>> allQueues = new();

            foreach (CellFinderDirection direction in cellFinder.Directions)
            {
                allQueues.Add(CaptureTerritoryQueue(botView, cellFinder.Cell.Location, direction));
            }

            int maxIndex = 0;
            int maxDistance = 0;
            for (int i = 0; i < allQueues.Count; i++)
            {
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

        private static List<MovementQueueItem> CaptureTerritoryQueue(BotView botView, Location cell, CellFinderDirection direction)
        {
            List<MovementQueueItem> destinationList = new();

            Location currentDirection = direction.Direction;
            Location currentLocation = cell;

            int directionMax = 4;

            for (int directionChangeCount = 0;  directionChangeCount < 4; directionChangeCount++)
            {
                //increase the max to avoid colliding with ourselves
                if (directionChangeCount == 2)
                {
                    directionMax++;
                }

                //find a valid destination
                int directionMagnitude = directionMax;
                Location tempLocation = currentLocation.Move(currentDirection, directionMagnitude);

                //stay in bounds
                while(botView.GetCellByLocation(tempLocation)?.CellType == CellType.OutOfBounds && directionMagnitude > 1)
                {
                    directionMagnitude--;
                    tempLocation = currentLocation.Move(currentDirection, directionMagnitude);
                }

                if (tempLocation != currentDirection)
                {
                    destinationList.Add(new(tempLocation) { CaptureRotation = direction.Rotation});
                }

                currentLocation = tempLocation;

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

            return new() { new(tempLocation) };
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

        public static bool AreMovementActionsSafe(this List<MovementAction> actions)
        {
            return true;
        }

    }
}
