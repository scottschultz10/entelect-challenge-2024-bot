using SproutReferenceBot.Enums;
using SproutReferenceBot.Models;

namespace SproutReferenceBot.Services;
public class BotService
{
    private Guid botId;
    private BotStateDTO? botState;
    private readonly BotView botView;
    private List<BotViewCell> clockwiseView;
    private bool hasReceivedBotState = false;

    private List<MovementQueueItem> movementQueue;
    private MovementQueueItem? sideTrackMovementItem;
    private List<MovementAction> movementActions;
    private BotViewCell? centerCell;

    private Location? startingLocation;

    public BotService()
    {
        botView = new();
        clockwiseView = new();
        movementQueue = new();
        movementActions = new();
    }

    public BotCommand ProcessState()
    {
        hasReceivedBotState = false;

        if (BotServiceHelpers.LastDirection == BotAction.IDLE)
        {
            //bot has been reset. Clear everything
            movementQueue = new();
            sideTrackMovementItem = null;
            BotServiceHelpers.Goal = BotGoal.NONE;
        }

        centerCell = botView.CenterCell();
        Console.WriteLine($"Center Cell = {centerCell.Location}");
        //Console.WriteLine($"PowerUps = {botState!.PowerUp}");
        //Console.WriteLine($"Super PowerUps = {botState.SuperPowerUp}");

        Console.WriteLine($"Bot Positions: {string.Join(", ", botState!.BotPostions?.Select(x => x.ToString()) ?? [])}");
        Console.WriteLine($"Leaderboard: {string.Join(", ", botState!.LeaderBoard?.Select(x => x.Key + " " + x.Value) ?? [])}");

        if (movementQueue.Count > 0 && centerCell.CellType == BotServiceHelpers.MyTerritory && BotServiceHelpers.Goal == BotGoal.Capture)
        {
            //if I am standing in my own territory after capture. Cancel all queued actions and do something new
            movementQueue = new();
        }

        BotCommand? botCommand;
        if (BotServiceHelpers.Goal != BotGoal.MoveAlongLine && BotServiceHelpers.Goal != BotGoal.MoveToLine && BotServiceHelpers.Goal != BotGoal.MoveToCorner)
        {
            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        //find a corner that matches BotServiceHelpers.MyTerritory
        CellFinderResult? cornerCell = CellFinderService.FindCornerCell(botView, clockwiseView);
        CellFinderResult? lineCell = CellFinderService.FindLineCell(botView, clockwiseView);

        Console.WriteLine($"Corner Found = {cornerCell?.Cell.Location} == {centerCell.Location} {cornerCell != null && cornerCell.Cell.Location == centerCell.Location}");
        Console.WriteLine($"Line Found = {lineCell?.Cell.Location}");

        //if there are any other bots within view, don't capture
        bool anyVisibleBots = clockwiseView.Any(x => x.HasBot && !x.IsMe);

        if (!anyVisibleBots)
        {
            if (cornerCell != null && cornerCell.Cell.Location == centerCell.Location)
            {
                movementQueue = BotMovementService.CaptureTerritory(cornerCell, botView);
                BotServiceHelpers.Goal = BotGoal.Capture;

                botCommand = CommandFromQueue();
                if (botCommand != null) return botCommand;
            }

            if (lineCell != null && lineCell.Cell.Location == centerCell.Location && lineCell.CanCapture)
            {
                movementQueue = BotMovementService.CaptureTerritory(lineCell, botView);
                BotServiceHelpers.Goal = BotGoal.Capture;

                botCommand = CommandFromQueue();
                if (botCommand != null) return botCommand;
            }
        }

        if (cornerCell != null)
        {
            if (cornerCell.HasPriority || lineCell == null)
            {
                movementQueue = new() { new(cornerCell.Cell.Location, new(centerCell.Location.CommonDirection(cornerCell.Cell.Location), centerCell.Location.RotationFromDestination(cornerCell.Cell.Location))) };
                BotServiceHelpers.Goal = BotGoal.MoveToCorner;

                botCommand = CommandFromQueue();
                if (botCommand != null) return botCommand;
            }
        }

        //can't find a corner. Find a line instead
        if (lineCell != null)
        {
            if (lineCell.Cell.Location == centerCell.Location)
            {
                if (lineCell.CanCapture && !anyVisibleBots)
                {
                    movementQueue = BotMovementService.CaptureTerritory(lineCell, botView);
                    BotServiceHelpers.Goal = BotGoal.Capture;
                }
                else
                {
                    movementQueue = BotMovementService.MoveAlongLine(lineCell, botView);
                    BotServiceHelpers.Goal = BotGoal.MoveAlongLine;
                }
            }
            else
            {
                movementQueue = new() { new(lineCell.Cell.Location, lineCell.Directions.First()) };
                BotServiceHelpers.Goal = BotGoal.MoveToLine;
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
        List<BotViewCell> safeCells = botView.CellPrimaryBufferByLocation(centerCell.Location).FindAll(x => !x.IsHazard);

        if (safeCells.Count > 0)
        {
            return new()
            {
                BotId = botId,
                Action = safeCells[randDirection.Next(0, safeCells.Count)].Location.Difference(centerCell.Location).ToBotAction(),
            };
        }
        else
        {
            return new()
            {
                BotId = botId,
                Action = (BotAction)randDirection.Next(1, 5),
            };
        }
    }

    private BotCommand? CommandFromQueue()
    {
        if (BotServiceHelpers.LastDirection != BotAction.IDLE)
        {
            //not capturing and in my territory - look for side tracks within my territory
            if (centerCell!.CellType == BotServiceHelpers.MyTerritory && BotServiceHelpers.Goal != BotGoal.Capture && sideTrackMovementItem == null)
            {
                List<BotViewCell> coneView = botView.CenterCellConeView(BotServiceHelpers.LastDirection.ToLocationDirection());

                if (coneView.Any(x => (x.IsTrail && x.CellType != BotServiceHelpers.MyTrail) || x.PowerUpType != PowerUpType.NONE))
                {
                    //look for powerups and other bot trails first
                    sideTrackMovementItem = new(coneView.First(x => (x.IsTrail && x.CellType != BotServiceHelpers.MyTrail) || x.PowerUpType != PowerUpType.NONE).Location, new(BotServiceHelpers.LastDirection.ToLocationDirection(), RotationDirection.Clockwise));
                }
            }
            else if (movementQueue.Count > 0 && sideTrackMovementItem == null)
            {
                //else look for normal capture side tracks
                MovementQueueItem tempMovement = movementQueue.First();

                //Check in the cone view for the newest destination for a side track
                List<BotViewCell> coneView = botView.CenterCellConeView(tempMovement.Direction.Direction);
                //add a cone view for the current direction as well - less priority
                coneView.AddRange(botView.CenterCellConeView(BotServiceHelpers.LastDirection.ToLocationDirection()));

                //get a view of all cells in a line in the last direction
                List<BotViewCell> lineView = botView.CenterCellDirectionView(BotServiceHelpers.LastDirection.ToLocationDirection());

                if (coneView.Any(x => (x.IsTrail && x.CellType != BotServiceHelpers.MyTrail) || x.PowerUpType != PowerUpType.NONE))
                {
                    //look for powerups and other bot trails first
                    sideTrackMovementItem = new(coneView.First(x => (x.IsTrail && x.CellType != BotServiceHelpers.MyTrail) || x.PowerUpType != PowerUpType.NONE).Location, tempMovement.Direction);
                }
                else if (BotServiceHelpers.Goal == BotGoal.Capture && centerCell.CellType != BotServiceHelpers.MyTerritory && lineView.Any(x => x.CellType == BotServiceHelpers.MyTerritory))
                {
                    //then look for my territory if I am capturing - find close cell to stop capturing at
                    sideTrackMovementItem = new(lineView.First(x => x.CellType == BotServiceHelpers.MyTerritory).Location, new(BotServiceHelpers.LastDirection.ToLocationDirection(), tempMovement.Direction.Rotation));
                }

                //offset the destination to avoid going back on myself - only if there is still more than one destination left
                if (sideTrackMovementItem != null && movementQueue.Count > 1)
                {
                    movementQueue[0].Destination = movementQueue[0].Destination.MoveOffset(sideTrackMovementItem.Destination.Difference(centerCell!.Location), movementQueue[0].Direction.Direction);
                }
            }
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
                BotViewCell? sideTrackCell = botView.CellByLocation(sideTrackMovementItem.Destination);
                if (sideTrackMovementItem.Destination == centerCell!.Location
                    || sideTrackCell == null
                    || (!sideTrackCell.IsTrail && sideTrackCell.PowerUpType == PowerUpType.NONE && sideTrackCell.CellType != BotServiceHelpers.MyTerritory))
                {
                    //reset side track - move back to the normal movementQueue
                    thisMovement = null;
                    sideTrackMovementItem = null;
                    movementActions = new();

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
                while (thisMovement.Destination == centerCell!.Location)
                {
                    //we are at the destination - reset movements
                    movementActions = new();

                    movementQueue.RemoveAt(0);
                    if (movementQueue.Count > 0)
                    {
                        thisMovement = movementQueue.First();
                    }
                    else return null;
                }
            }

            if (movementActions.Count <= 0 || !movementActions.AreMovementActionsSafe(botView))
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

                Console.WriteLine($"Goal - {BotServiceHelpers.Goal}");
                Console.WriteLine($"!!COMMAND - {botAction}");
                BotCommand command = new()
                {
                    BotId = botId,
                    Action = botAction,
                };
                return command;
            }
        }

        return null;
    }

    public void SetBotId(Guid NewBotId)
    {
        botId = NewBotId;
    }

    public Guid GetBotId()
    {
        return botId;
    }

    public void SetBotState(BotStateDTO botState)
    {
        this.botState = botState;
        this.botView.SetBotView(botState);
        clockwiseView = CellFinderService.GetClockwiseView(botView);

        BotServiceHelpers.LastDirection = botState.DirectionState;

        if (BotServiceHelpers.LastDirection == BotAction.IDLE)
        {
            BotServiceHelpers.MyTerritory = botView.CenterCell().CellType;
            startingLocation = botView.CenterCell().Location;
        }

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
