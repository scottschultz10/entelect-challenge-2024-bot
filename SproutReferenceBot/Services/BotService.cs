using SproutReferenceBot.Enums;
using SproutReferenceBot.Extensions;
using SproutReferenceBot.Globals;
using SproutReferenceBot.Models;

namespace SproutReferenceBot.Services;
public class BotService
{
    private BotStateDTO? botState;
    private BotView botView;
    private bool hasReceivedBotState = false;

    private List<MovementQueueItem> movementQueue;
    private MovementQueueItem? sideTrackMovementItem;
    private List<MovementAction> movementActions;
    private HashSet<Location> blacklistLocations;
    private BotViewCell? centerCell;

    private Location? startingLocation;
    private BotAggression botAggression;

    /// <summary>
    /// if there are any other bots within view, don't capture
    /// </summary>
    private bool AnyVisibleBots => botView.ClockwiseView.Any(x => x.HasBot && !x.IsMe);
    /// <summary>
    /// Count the number of times a side track has happened. To prevent continuous sidetracking. Resets when entering MyTerritory
    /// </summary>
    private int SideTrackCounter = 0;

    public BotService()
    {
        botView = new();
        movementQueue = [];
        movementActions = [];
        blacklistLocations = [];
        botAggression = BotAggression.None;
    }

    public BotCommand ProcessState()
    {
        hasReceivedBotState = false;
        centerCell = botView.CenterCell();
        botAggression = BotAggressionSvc.BotAggressionLevel(botState!.LeaderBoard);

        if (BotServiceGlobals.LastDirection == BotAction.IDLE)
        {
            //bot has been reset. Clear everything
            movementQueue = new();
            sideTrackMovementItem = null;
            SideTrackCounter = 0;
            BotServiceGlobals.Goal = BotGoal.NONE;
        }

        Console.WriteLine($"Center Cell = {centerCell.Location}");
        //Console.WriteLine($"PowerUps = {botState!.PowerUp}");
        //Console.WriteLine($"Super PowerUps = {botState.SuperPowerUp}");

        Console.WriteLine($"Bot Positions: {string.Join(", ", botState!.BotPostions?.Select(x => x.ToString()) ?? [])}");
        Console.WriteLine($"Leaderboard: {string.Join(",\n", botState!.LeaderBoard?.Select(x => x.Key + " " + x.Value) ?? [])}");

        Console.WriteLine($"Clockwise View: {botView.ClockwiseView.Count}");

        if (movementQueue.Count > 0 && centerCell.CellType == BotServiceGlobals.MyTerritory && BotServiceGlobals.Goal == BotGoal.Capture)
        {
            //if I am standing in my own territory after capture. Cancel all queued actions and do something new
            if (BotServiceGlobals.LastDirection != BotAction.IDLE)
            {
                //check if I actually captured territory. If not mark it as a no go capture. Probably a spawn
                BotViewCell? behindCell = (centerCell.Location.Move(BotServiceGlobals.LastDirection.ToLocationDirection().OppositeDirection())).ToBotViewCell();

                if (behindCell != null && behindCell.CellType != BotServiceGlobals.MyTerritory)
                {
                    //did not capture / blacklist all visible cells of matching type in the botView
                    foreach (BotViewCell blacklistCell in botView.ClockwiseView.FindAll(x => x.CellType == behindCell.CellType))
                    {
                        //blacklist the buffers as well. To keep clear
                        foreach (BotViewCell buffer in blacklistCell.Location.CellBuffer())
                        {
                            blacklistLocations.Add(buffer.Location);
                        }
                    }
                }
            }

            movementQueue = new();
            SideTrackCounter = 0;
        }


        BotCommand? botCommand;
        if (BotServiceGlobals.Goal != BotGoal.MoveAlongLine && BotServiceGlobals.Goal != BotGoal.MoveToCaptureCell)
        {
            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        //find a cell that matches BotServiceHelpers.MyTerritory, from which to capture territory
        CellFinderResult? captureCell = CellFinderService.FindCaptureCell(botView);

        Console.WriteLine($"Capture Cell Found = {captureCell?.Cell.Location} == {centerCell.Location} {captureCell != null && captureCell.Cell.Location == centerCell.Location}");

        //reset - assume we are in MyTerritory if we reach this point - no goal
        SideTrackCounter = 0;

        if (captureCell != null)
        {
            //cannot capture this location, it has been blacklisted previously
            bool isCaptureBlacklisted = blacklistLocations.Contains(centerCell.Location);
            //aggression level forbids capturing
            bool aggressionNotNone = botAggression != BotAggression.None;

            //combine above checks
            bool isCenterCellCapturable = !AnyVisibleBots && !isCaptureBlacklisted && aggressionNotNone;

            if (captureCell.Cell.Location == centerCell.Location)
            {
                if (captureCell.CanCapture && isCenterCellCapturable)
                {
                    movementQueue = BotMovementService.CaptureTerritory(captureCell, botAggression);
                    BotServiceGlobals.Goal = BotGoal.Capture;
                }
                else
                {
                    movementQueue = BotMovementService.MoveAlongLine(botView);
                    BotServiceGlobals.Goal = BotGoal.MoveAlongLine;
                }
            }
            else
            {
                movementQueue = new() { new(captureCell.Cell.Location, captureCell.Directions.First()) };
                BotServiceGlobals.Goal = BotGoal.MoveToCaptureCell;
            }
        }

        botCommand = CommandFromQueue();
        if (botCommand != null) return botCommand;

        //return to starting location to try and find something
        movementQueue = new() { new(startingLocation!, new(centerCell.Location.CommonDirection(startingLocation!), centerCell.Location.RotationFromDestination(startingLocation!))) };

        botCommand = CommandFromQueue();
        if (botCommand != null) return botCommand;

        //finally travel in a random direction that is safe
        Random randDirection = new();
        List<BotViewCell> safeCells = centerCell.Location.CellPrimaryBuffer().FindAll(x => !x.IsHazard);

        if (safeCells.Count > 0)
        {
            return new()
            {
                BotId = BotServiceGlobals.BotID,
                Action = safeCells[randDirection.Next(0, safeCells.Count)].Location.Difference(centerCell.Location).ToBotAction(),
            };
        }
        else
        {
            return new()
            {
                BotId = BotServiceGlobals.BotID,
                Action = (BotAction)randDirection.Next(1, 5),
            };
        }
    }

    private BotCommand? CommandFromQueue()
    {
        if (BotServiceGlobals.LastDirection != BotAction.IDLE)
        {
            //not capturing and in my territory - look for side tracks within my territory
            if (centerCell!.CellType == BotServiceGlobals.MyTerritory && BotServiceGlobals.Goal != BotGoal.Capture && sideTrackMovementItem == null)
            {
                List<BotViewCell> coneView = botView.CenterCellConeView(BotServiceGlobals.LastDirection.ToLocationDirection(), botAggression);

                if (coneView.Any(x => x.IsTrail && x.CellType != BotServiceGlobals.MyTrail))
                {
                    //look for other bot trails first
                    sideTrackMovementItem = new(coneView.First(x => (x.IsTrail && x.CellType != BotServiceGlobals.MyTrail)).Location, new(BotServiceGlobals.LastDirection.ToLocationDirection(), RotationDirection.Clockwise));
                }
                else if (!AnyVisibleBots && SideTrackCounter == 0 && coneView.Any(x => x.PowerUpType != PowerUpType.NONE))
                {
                    //then look for powerups
                    //only get powerups if no visible bots
                    sideTrackMovementItem = new(coneView.First(x => x.PowerUpType != PowerUpType.NONE).Location, new(BotServiceGlobals.LastDirection.ToLocationDirection(), RotationDirection.Clockwise));
                }

                //offset the destination to avoid going back on myself - only if there is still more than one destination left
                if (sideTrackMovementItem != null && movementQueue.Count > 1)
                {
                    movementQueue[0].Destination = movementQueue[0].Destination.MoveOffset(sideTrackMovementItem.Destination.Difference(movementQueue[0].Destination), movementQueue[0].Direction.Direction);
                }
            }
            else if (movementQueue.Count > 0 && sideTrackMovementItem == null)
            {
                //else look for normal capture side tracks
                MovementQueueItem tempMovement = movementQueue.First();

                //Check in the cone view for the newest destination for a side track
                List<BotViewCell> coneView = botView.CenterCellConeView(tempMovement.Direction.Direction, botAggression);
                //add a cone view for the current direction as well - less priority
                coneView.AddRange(botView.CenterCellConeView(BotServiceGlobals.LastDirection.ToLocationDirection(), botAggression));

                //get a view of all cells in a line in the last direction
                List<BotViewCell> lineView = botView.CenterCellDirectionView(BotServiceGlobals.LastDirection.ToLocationDirection());

                if (coneView.Any(x => x.IsTrail && x.CellType != BotServiceGlobals.MyTrail))
                {
                    //look for other bot trails first
                    sideTrackMovementItem = new(coneView.First(x => (x.IsTrail && x.CellType != BotServiceGlobals.MyTrail)).Location, tempMovement.Direction);
                }
                //don't perform other sidetracks if any other bots are visible
                else if (!AnyVisibleBots && SideTrackCounter == 0)
                {
                    if (coneView.Any(x => x.PowerUpType != PowerUpType.NONE))
                    {
                        //look for powerups
                        sideTrackMovementItem = new(coneView.First(x => x.PowerUpType != PowerUpType.NONE).Location, tempMovement.Direction);
                    }
                    else if (BotServiceGlobals.Goal == BotGoal.Capture && centerCell.CellType != BotServiceGlobals.MyTerritory && lineView.Any(x => x.CellType == BotServiceGlobals.MyTerritory))
                    {
                        //then look for my territory if I am capturing - find close cell to stop capturing at
                        //if any visible bots, Abort will take place during capture anyway
                        sideTrackMovementItem = new(lineView.First(x => x.CellType == BotServiceGlobals.MyTerritory).Location, new(BotServiceGlobals.LastDirection.ToLocationDirection(), tempMovement.Direction.Rotation));
                    }
                }

                //offset the destination to avoid going back on myself - only if there is still more than one destination left
                if (sideTrackMovementItem != null && movementQueue.Count > 1)
                {
                    movementQueue[0].Destination = movementQueue[0].Destination.MoveOffset(sideTrackMovementItem.Destination.Difference(movementQueue[0].Destination), movementQueue[0].Direction.Direction);
                }
            }
        }

        //look out for bots while capturing and abort
        //unless there is a side track to capture a trail
        if (botView.ClockwiseView.Any(x => x.HasBot && !x.IsMe)
            && BotServiceGlobals.Goal == BotGoal.Capture && centerCell!.CellType != BotServiceGlobals.MyTerritory
            && (movementQueue.Count > 0 || sideTrackMovementItem == null || !(sideTrackMovementItem.Destination.ToBotViewCell()?.IsTrail ?? false && sideTrackMovementItem.Destination.ToBotViewCell()?.CellType != BotServiceGlobals.MyTrail)))
        {
            MovementQueueItem? tempMovementItem = movementQueue.FirstOrDefault() ?? sideTrackMovementItem;

            sideTrackMovementItem = null;

            //find the closest myTerritory - by looking in cone view (same as side track)
            //use High Aggression for max view cone

            //Check in the cone view for the newest destination for a side track
            List<BotViewCell> coneView = botView.CenterCellConeView(tempMovementItem?.Direction.Direction ?? BotServiceGlobals.LastDirection.ToLocationDirection(), BotAggression.High);
            //add a cone view for the current direction as well - less priority
            coneView.AddRange(botView.CenterCellConeView(BotServiceGlobals.LastDirection.ToLocationDirection(), BotAggression.High));

            BotViewCell? myTerritoryCell = coneView.Where(x => x.CellType == BotServiceGlobals.MyTerritory).FirstOrDefault();

            if (myTerritoryCell != null)
            {
                movementQueue = [new(myTerritoryCell.Location, new(centerCell!.Location.CommonDirection(myTerritoryCell.Location), tempMovementItem?.Direction.Rotation ?? RotationDirection.Clockwise))];
            }
            //else continue with normal queue

            Console.Write($"Aborting Capture - new destination {myTerritoryCell?.Location.ToString() ?? "None"}");
        }

        //queue has items. Move along the queue / or i have a sideTrack
        if (movementQueue.Count > 0 || sideTrackMovementItem != null)
        {
            /*TODO: For sidetrack. Expand on when the bot checks for side track 
             * Increase cone view, add more surrounding cells close to bot
            */

            //check for side track first
            MovementQueueItem? thisMovement = null;
            if (sideTrackMovementItem != null)
            {
                //Reached the side track
                //or the side track is no longer there
                BotViewCell? sideTrackCell = (sideTrackMovementItem.Destination).ToBotViewCell();
                if (sideTrackMovementItem.Destination == centerCell!.Location
                    || sideTrackCell == null
                    || (!sideTrackCell.IsTrail && sideTrackCell.PowerUpType == PowerUpType.NONE && sideTrackCell.CellType != BotServiceGlobals.MyTerritory))
                {
                    //reset side track - move back to the normal movementQueue
                    thisMovement = null;
                    sideTrackMovementItem = null;
                    movementActions = new();
                    SideTrackCounter++;

                    //no other movements in the queue - so just exit
                    if (movementQueue.Count == 0)
                        return null;
                }
                else
                {
                    thisMovement = sideTrackMovementItem;
                }
            }

            if (thisMovement == null)
            {
                thisMovement = movementQueue.First();
                while (thisMovement.Destination == centerCell!.Location || centerCell.Location.HasGonePastDestination(thisMovement.Destination, thisMovement.Direction.Direction))
                {
                    //we are at the destination - reset movements
                    //or we have gone past it
                    movementActions = new();

                    movementQueue.RemoveAt(0);
                    if (movementQueue.Count > 0)
                    {
                        thisMovement = movementQueue.First();
                    }
                    else return null;
                }
            }

            if (movementActions.Count <= 0 || !movementActions.AreMovementActionsSafe())
            {
                var moveToDestination = BotMovementService.MoveToDestination(centerCell!.Location, thisMovement.Destination, botView, thisMovement.Direction.Rotation);
                movementActions = moveToDestination.MovementActions;

                //TODO
                //ONLY CHANGE THE DIRECTION, IF I AM NOW PASSED THE DESTINATION. I.E. MOVING THERE IS NOT PRIORITISED
                ////use the offset as the new destination, to prevent going back on yourself
                if (moveToDestination.HasBeenOffset)
                {
                    Console.WriteLine($"Offsetting the by: {moveToDestination.OffsetDifference} -> {thisMovement.Destination} = {thisMovement.Destination.Move(moveToDestination.OffsetDifference)}");
                    thisMovement.Destination = thisMovement.Destination.MoveOffset(moveToDestination.OffsetDifference, thisMovement.Direction.Direction);
                }
            }

            if (movementActions.Count > 0)
            {
                var tempActions = BotMovementService.ValidateMovementActions(movementActions, botView);

                if (tempActions.Actions.Count <= 0)
                {
                    //no more actions for this destination. Move to the next
                    thisMovement.Destination = centerCell!.Location;
                    movementActions = new();
                    Console.WriteLine($"No Valid Movements: new destination {thisMovement.Destination}");
                    return CommandFromQueue();
                }
                else if (!tempActions.AllValid)
                {
                    Console.WriteLine($"Changing the destination: {thisMovement.Destination} -> {movementActions.Last().Location}");
                    //change the destination
                    thisMovement.Destination = movementActions.Last().Location;
                }
            }

            Console.WriteLine($"Movement Destination : {thisMovement.Destination}");
            Console.WriteLine($"Queue = {string.Join("; ", movementQueue.Select(x => x.Destination))}");
            Console.WriteLine($"Side Track = {sideTrackMovementItem?.Destination}");

            if (movementActions.Count > 0)
            {
                //keep going through the queue
                BotAction botAction = movementActions.First().Action;
                movementActions.RemoveAt(0);

                Console.WriteLine($"Goal - {BotServiceGlobals.Goal}");
                Console.WriteLine($"!!COMMAND - {botAction}");
                BotCommand command = new()
                {
                    BotId = BotServiceGlobals.BotID,
                    Action = botAction,
                };
                return command;
            }
        }

        return null;
    }

    public static void SetBotId(Guid NewBotId)
    {
        BotServiceGlobals.BotID = NewBotId;
    }

    public static Guid GetBotId()
    {
        return BotServiceGlobals.BotID;
    }

    public void SetBotState(BotStateDTO botState)
    {
        this.botState = botState;
        botView = new(botState);

        BotServiceGlobals.LastDirection = botState.DirectionState;

        if (BotServiceGlobals.LastDirection == BotAction.IDLE)
        {
            BotServiceGlobals.MyTerritory = botView.CenterCell().CellType;
            startingLocation = botView.CenterCell().Location;
        }

        BotServiceGlobals.GameTick = botState.GameTick;

        hasReceivedBotState = true;
    }

    public BotStateDTO? GetBotState()
    {
        return botState;
    }

    public bool HasReceivedBotState()
    {
        return botState != null && hasReceivedBotState;
    }

    public string PrintBotView()
    {
        return botView.PrintBotView();
    }
}
