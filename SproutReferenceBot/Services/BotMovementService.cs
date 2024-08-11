using SproutReferenceBot.Enums;
using SproutReferenceBot.Extensions;
using SproutReferenceBot.Globals;
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

        /// <summary>
        /// Only do the basic movement to a destination. No additional checks, used for some simple movement validations
        /// </summary>
        public static List<MovementAction> MoveToDestinationBasic(Location current, Location destination)
        {
            Location difference = current.Difference(destination);

            List<MovementAction> movementList = new();

            //limit the movement to 4 squares - set movement within botView
            while (difference != LocationDirection.NONE)
            {
                if (difference.Y > 0)
                {
                    difference = difference.Move(LocationDirection.Up);
                    current = current.Move(LocationDirection.Up);
                    movementList.Add(new(BotAction.Up, current));
                }
                else if (difference.Y < 0)
                {
                    difference = difference.Move(LocationDirection.Down);
                    current = current.Move(LocationDirection.Down);
                    movementList.Add(new(BotAction.Down, current));
                }
                else if (difference.X > 0)
                {
                    difference = difference.Move(LocationDirection.Left);
                    current = current.Move(LocationDirection.Left);
                    movementList.Add(new(BotAction.Left, current));
                }
                else if (difference.X < 0)
                {
                    difference = difference.Move(LocationDirection.Right);
                    current = current.Move(LocationDirection.Right);
                    movementList.Add(new(BotAction.Right, current));
                }
            }

            return movementList;
        }

        public static (List<MovementAction> MovementActions, bool HasBeenOffset, Location OffsetDifference) MoveToDestination(Location current, Location destination, BotView botView, RotationDirection? captureRotation)
        {
            //make the basic movement first
            Location difference = current.Difference(destination);
            Location currentLocation = current;
            BotAction lastDirection = BotServiceGlobals.LastDirection;

            List<MovementAction> movementList = PopulateMovementActions(currentLocation, difference, botView, lastDirection, captureRotation);

            if (lastDirection == BotAction.IDLE)
            {
                lastDirection = movementList.FirstOrDefault()?.Action ?? (BotAction)(new Random().Next(1, 5));
            }

            Location offsetDirection = current.CommonDirection(destination);
            bool hasBeenOffset = false;
            //keep track of the offsets for when we start repeating ourselves
            List<Location> allOffsets = new();

            //then check hazards on trail / and avoid
            while (movementList.Count > 0 && !AreMovementActionsSafe(movementList))
            {
                MovementAction actionToOffset = movementList.Last();

                //go the opposite direction of the rotation
                //offset by the most common direction
                Location offset;

                if (captureRotation == RotationDirection.Clockwise)
                {
                    offset = actionToOffset.Location.Move(offsetDirection, 1);
                }
                else
                {
                    offset = actionToOffset.Location.Move(offsetDirection, 1);
                }

                Console.WriteLine($"Offsetting the movement: {offset}");

                if (allOffsets.Contains(offset))
                {
                    //no valid offsets will save us. Accept death
                    Console.WriteLine("Accepting Death");
                    return (movementList, hasBeenOffset, movementList.Last().Location.Difference(currentLocation));
                }

                allOffsets.Add(offset);

                //populate from current to offset
                List<MovementAction> tempActions = PopulateMovementActions(currentLocation, currentLocation.Difference(offset), botView, lastDirection, captureRotation);

                //can't offset anymore. Change the direction more to allow more offsets
                if (tempActions.Last().Location == movementList.Last().Location)
                {
                    if (captureRotation == RotationDirection.Clockwise)
                    {
                        offsetDirection = offsetDirection.NextCounterClockwiseDirection();
                    }
                    else
                    {
                        offsetDirection = offsetDirection.NextClockwiseDirection();
                    }
                }

                movementList = tempActions;
                hasBeenOffset = true;
            }

            return (movementList, hasBeenOffset, movementList.Last().Location.Difference(currentLocation));
        }

        private static List<MovementAction> PopulateMovementActions(Location currentLocation, Location difference, BotView botView, BotAction lastDirection, RotationDirection? captureRotation)
        {
            List<MovementAction> movementList = new();

            //limit the movement to 4 squares - set movement within botView
            while (difference != LocationDirection.NONE && movementList.Count < 4)
            {
                bool hasMoved = false;
                List<MovementAction> possibleMovements = new();

                if (difference.Y > 0)
                {
                    possibleMovements.Add(new(BotAction.Up, currentLocation.Move(LocationDirection.Up)));
                }
                if (difference.Y < 0)
                {
                    possibleMovements.Add(new(BotAction.Down, currentLocation.Move(LocationDirection.Down)));
                }
                if (difference.X > 0)
                {
                    possibleMovements.Add(new(BotAction.Left, currentLocation.Move(LocationDirection.Left)));
                }
                if (difference.X < 0)
                {
                    possibleMovements.Add(new(BotAction.Right, currentLocation.Move(LocationDirection.Right)));
                }

                if (possibleMovements.Count > 0)
                {
                    MovementAction? newMovement = PrioritiseMovementAction(currentLocation, possibleMovements, lastDirection);

                    if (newMovement != null)
                    {
                        difference = difference.Move(newMovement.Action.ToLocationDirection());
                        currentLocation = newMovement.Location;
                        lastDirection = newMovement.Action;
                        hasMoved = true;
                        movementList.Add(newMovement);
                    }
                }

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

            return movementList;
        }

        private static MovementAction? PrioritiseMovementAction(Location currentLocation, List<MovementAction> possibleActions, BotAction lastDirection)
        {
            return (from move in possibleActions
                    where PossibleBotAction(move.Action, lastDirection)
                    select new
                    {
                        move,
                        Direction = currentLocation.DirectionPriority(move.Location, lastDirection),
                        CellType = _PrioritisedCellTypes(move.Location.ToBotViewCell()),
                    }).OrderBy(x => x.Direction).ThenBy(x => x.CellType).FirstOrDefault()?.move;

            static bool PossibleBotAction(BotAction action, BotAction lastDirection)
            {
                return ((action == BotAction.Up && lastDirection != BotAction.Down)
                        || (action == BotAction.Right && lastDirection != BotAction.Left)
                        || (action == BotAction.Down && lastDirection != BotAction.Up)
                        || (action == BotAction.Left && lastDirection != BotAction.Right));
            }

            static int _PrioritisedCellTypes(BotViewCell? cell)
            {
                if (BotServiceGlobals.Goal != BotGoal.Capture)
                {
                    if (cell?.CellType == BotServiceGlobals.MyTerritory)
                    {
                        return -1;
                    }
                    else return 1;
                }
                else return 0;
            }
        }

        /// <summary>
        /// Ensure that the current route I am on is safe, if not, reset my destination. Similar to MoveToDestination, but to be executed after the MovementAction list is populated and we are moving through it
        /// </summary>
        /// <returns>A revised set of movements</returns>
        public static (List<MovementAction> Actions, bool AllValid) ValidateMovementActions(List<MovementAction> actions, BotView botView)
        {
            BotViewCell centerCell = botView.CenterCell();
            List<BotViewCell> actionCells = new();

            foreach (MovementAction action in actions)
            {
                BotViewCell? cell = action.Location.ToBotViewCell();

                if (cell != null)
                {
                    actionCells.Add(cell);
                }
            }

            List<BotViewCell> validCells = actionCells;

            //shorten the movement lists / and keep searching
            if (validCells.Any(x => x.CellType == CellType.OutOfBounds))
            {
                List<BotViewCell> tempCells = new();
                foreach (BotViewCell cell in validCells)
                {
                    //take out all outofbounds
                    if (cell.CellType != CellType.OutOfBounds)
                    {
                        tempCells.Add(cell);
                    }
                    else break;
                }

                validCells = tempCells;
            }

            if (BotServiceGlobals.Goal == BotGoal.Capture && validCells.Any(x => x.CellType == BotServiceGlobals.MyTerritory))
            {
                List<BotViewCell> tempCells = new();
                foreach (BotViewCell cell in validCells)
                {
                    tempCells.Add(cell);
                    //keep the last BotServiceHelpers.MyTerritory as the destination
                    if (cell.CellType == BotServiceGlobals.MyTerritory)
                    {
                        break;
                    }
                }

                validCells = tempCells;
            }

            //if more than 3 sides are hazards then take it out. Can't go in and out safely
            if (validCells.Any(x => x.Location.CellPrimaryBuffer().Count(x => x.IsHazard) >= 3))
            {
                List<BotViewCell> tempCells = new();
                foreach (BotViewCell cell in validCells)
                {
                    tempCells.Add(cell);
                    //take out the hazardous cell
                    if (cell.Location.CellPrimaryBuffer().Count(x => x.IsHazard) < 3)
                    {
                        break;
                    }
                }

                validCells = tempCells;
            }

            if (validCells.Count == 0)
            {
                return (new(), false);
            }

            //movement is still the same
            if (validCells.Count == actionCells.Count)
            {
                return (actions, true);
            }
            else
            {
                List<MovementAction> returnActions = new();
                foreach (BotViewCell cell in validCells)
                {
                    returnActions.Add(actions.First(x => x.Location == cell.Location));
                }

                return (returnActions, false);
            }
        }


        public static List<MovementQueueItem> CaptureTerritory(CellFinderResult cellFinder, BotAggression aggression)
        {
            //TODO - use last direction to pick the capture. ORDERBY

            List<List<MovementQueueItem>> allQueues = new();

            foreach (CellFinderDirection direction in cellFinder.Directions)
            {
                allQueues.Add(CaptureTerritoryQueue(cellFinder.Cell.Location, direction, aggression));
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

        private static List<MovementQueueItem> CaptureTerritoryQueue(Location cell, CellFinderDirection direction, BotAggression aggression)
        {
            if (aggression == BotAggression.None)
            {
                return [];
            }

            List<MovementQueueItem> destinationList = new();

            Location currentDirection = direction.Direction;
            Location currentLocation = cell;

            Queue<int> directionLengths = DirectionLengthQueue(aggression);

            while (directionLengths.Count > 0)
            {
                //increase the max to avoid colliding with ourselves
                int directionMax = directionLengths.Dequeue();

                List<(Location Location, BotViewCell? Cell)?> cells = new();
                for (int i = 1; i <= directionMax; i++)
                {
                    Location cellLocation = currentLocation.Move(currentDirection, i);
                    cells.Add((cellLocation, cellLocation.ToBotViewCell()));
                }

                //find a valid destination
                //stay in bounds
                Location tempLocation;

                if (cells.Any(x => x?.Cell?.CellType == CellType.OutOfBounds))
                {
                    tempLocation = cells.LastOrDefault(x => x?.Cell?.CellType != CellType.OutOfBounds)?.Location ?? currentLocation;
                }
                else if (cells.Any(x => x?.Cell?.CellType == BotServiceGlobals.MyTerritory))
                {
                    tempLocation = cells.LastOrDefault(x => x?.Cell?.CellType == BotServiceGlobals.MyTerritory)?.Location ?? currentLocation;
                }
                else
                {
                    tempLocation = cells.LastOrDefault()?.Location ?? currentLocation;
                }

                if (tempLocation.ToBotViewCell()?.CellType != CellType.OutOfBounds && tempLocation != currentDirection)
                {
                    destinationList.Add(new(tempLocation, new(currentDirection, direction.Rotation)));
                    currentLocation = tempLocation;

                    //we have hit BotServiceHelpers.MyTerritory, stop capturing
                    if (currentLocation.ToBotViewCell()?.CellType == BotServiceGlobals.MyTerritory)
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

        private static Queue<int> DirectionLengthQueue(BotAggression aggression)
        {
            int startingLength = (aggression) switch
            {
                BotAggression.None => 0,
                BotAggression.Low => 2,
                BotAggression.Medium => 3,
                BotAggression.High => 4,
                _ => 3
            };

            Queue<int> directionLengths = new();
            directionLengths.Enqueue(startingLength);
            directionLengths.Enqueue(startingLength + 2);
            directionLengths.Enqueue(startingLength + 1);
            directionLengths.Enqueue(startingLength + 2);

            return directionLengths;
        }

        public static List<MovementQueueItem> MoveAlongLine(BotView botView)
        {
            Location currentLocation = botView.CenterCell().Location;
            Location lastDirection = BotServiceGlobals.LastDirection.ToLocationDirection();
            //move in small increments
            int directionMagnitude = 2;


            List<BotViewCell> cellBuffer = currentLocation.CellPrimaryBuffer();

            //is line cell
            if (cellBuffer.Count(x => x.CellType == BotServiceGlobals.MyTerritory) == 3)
            {
                //cell that is not MyTerritory
                Location emptyDirection = currentLocation.CommonDirection(cellBuffer.First(x => x.CellType != BotServiceGlobals.MyTerritory).Location);

                if (lastDirection == emptyDirection.NextClockwiseDirection()
                    || lastDirection == emptyDirection.NextCounterClockwiseDirection())
                {
                    //already moving along the line. Keep going
                    Location moveLocation = MoveAlongLine_InBoundsLocation(currentLocation, lastDirection, directionMagnitude);
                    return [new(moveLocation, new(lastDirection, RotationDirection.Clockwise))];
                }
            }
            //if not, find a line cell in buffer (then match to clockwise directions)
            else
            {
                //only look at buffer cells that are in current direction, or are left / right
                List<Location> allowedDirections = [lastDirection, lastDirection.NextClockwiseDirection(), lastDirection.NextCounterClockwiseDirection()];

                foreach (BotViewCell cell in cellBuffer.Where(x => allowedDirections.Contains(currentLocation.CommonDirection(x.Location))))
                {
                    //is this buffer cell a line
                    if (cell.Location.CellPrimaryBuffer().Count(x => x.CellType == BotServiceGlobals.MyTerritory) == 3)
                    {
                        //move in the location of the buffer
                        Location moveLocation = MoveAlongLine_InBoundsLocation(currentLocation, currentLocation.CommonDirection(cell.Location), directionMagnitude);
                        return [new(moveLocation, new(lastDirection, RotationDirection.Clockwise))];
                    }
                }
            }

            //else find longest
            List<(Location location, RotationDirection rotation)> allLocations = [
                //current direction
                (MoveAlongLine_InBoundsLocation(currentLocation, lastDirection, directionMagnitude), RotationDirection.Clockwise),
                //clockwise direction
                (MoveAlongLine_InBoundsLocation(currentLocation, lastDirection.NextClockwiseDirection(), directionMagnitude), RotationDirection.Clockwise),
                //counter clockwise direction
                (MoveAlongLine_InBoundsLocation(currentLocation, lastDirection.NextCounterClockwiseDirection(), directionMagnitude), RotationDirection.CounterClockwise),
            ];

            //get the location that is the furthest away
            (Location longestLocation, RotationDirection longestRotation) = allLocations.OrderByDescending(x => currentLocation.DistanceTo(x.location)).First();

            return [new(longestLocation, new(currentLocation.CommonDirection(longestLocation), longestRotation))];
        }

        private static Location MoveAlongLine_InBoundsLocation(Location currentLocation, Location direction, int directionMagnitude)
        {
            Location tempLocation = currentLocation.Move(direction, directionMagnitude);

            List<MovementAction> movementActions = MoveToDestinationBasic(currentLocation, tempLocation);

            //get furthest cell away of celltype MyTerritory
            return movementActions.LastOrDefault(x => x.GetBotViewCell()?.CellType == BotServiceGlobals.MyTerritory)?.Location ?? currentLocation;
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
            List<BotViewCell> actionCells = actions.ToBotViewCells();

            if (actionCells.Any(x => x.IsHazard || (x.IsTrail && x.CellType == BotServiceGlobals.MyTrail)))
            {
                return false;
            }

            ////check the all buffers
            //if (actionCells.Count > 0)
            //{
            //    if (botView.CellBufferByLocation(actionCells.Last().Location).Any(x => x.HasWeed))
            //    {
            //        return false;
            //    }
            //}

            return true;
        }

    }
}
