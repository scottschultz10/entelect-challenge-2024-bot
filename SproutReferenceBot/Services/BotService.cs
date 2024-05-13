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

        if (botState!.DirectionState == BotAction.IDLE)
        {
            //bot has been reset. Clear everything
            movementQueue = new();
            BotServiceHelpers.Goal = BotGoal.NONE;
        }

        if (botState!.DirectionState != BotAction.IDLE)
        {
            //Console.WriteLine($"Cone View : {string.Join(", ", botView.CenterCellConeView(BotServiceHelpers.LastDirection.ToLocationDirection()).Select(x => x.Location))}");
        }

        centerCell = botView.CenterCell();
        Console.WriteLine($"Center Cell = {centerCell.Location}");
        Console.WriteLine($"PowerUps = {botState.PowerUp}");
        Console.WriteLine($"Super PowerUps = {botState.SuperPowerUp}");

        if (movementQueue.Count > 0 && centerCell.CellType == BotServiceHelpers.MyTerritory && BotServiceHelpers.Goal == BotGoal.Capture)
        {
            //if I am standing in my own territory after capture. Cancel all queued actions and do something new
            movementQueue = new();
        }

        BotCommand? botCommand;
        if (BotServiceHelpers.Goal != BotGoal.MoveAlongLine && BotServiceHelpers.Goal != BotGoal.MoveToLine)
        {
            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        //find a corner that matches BotServiceHelpers.MyTerritory
        CellFinderResult? cornerCell = CellFinderService.FindCornerCell(botView, clockwiseView);
        CellFinderResult? lineCell = CellFinderService.FindLineCell(botView, clockwiseView);

        Console.WriteLine($"Corner Found = {cornerCell?.Cell.Location}");
        Console.WriteLine($"Line Found = {lineCell?.Cell.Location}");

        if (cornerCell != null && cornerCell.Cell.Location == centerCell.Location)
        {
            movementQueue = BotMovementService.CaptureTerritory(cornerCell, botView);
            BotServiceHelpers.Goal = BotGoal.Capture;

            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        if (lineCell != null && lineCell.Cell.Location == centerCell.Location)
        {
            movementQueue = BotMovementService.CaptureTerritory(lineCell, botView);
            BotServiceHelpers.Goal = BotGoal.Capture;

            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
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
                if (lineCell.CanCapture)
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
        movementQueue = new() { new(startingLocation!, new (centerCell.Location.CommonDirection(startingLocation!), centerCell.Location.RotationFromDestination(startingLocation!))) };

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
        //queue has items. Move along the queue
        if (movementQueue.Count > 0)
        {
            MovementQueueItem thisMovement = movementQueue.First();

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

            if (movementActions.Count <= 0 || !movementActions.AreMovementActionsSafe(botView))
            {
                var moveToDestination = BotMovementService.MoveToDestination(centerCell.Location, thisMovement.Destination, botView, thisMovement.Direction.Rotation);
                movementActions = moveToDestination.MovementActions;

                //TODO
                //ONLY CHANGE THE DIRECTION, IF I AM NOW PASSED THE DESTINATION. I.E. MOVING THERE IS NOT PRIORITISED
                ////use the offset as the new destination, to prevent going back on yourself
                if (moveToDestination.HasBeenOffset)
                {
                    Console.WriteLine($"Offsetting the by: {moveToDestination.OffsetDifference} -> {thisMovement.Destination} = {thisMovement.Destination.Move(moveToDestination.OffsetDifference)}");
                    thisMovement.Destination = thisMovement.Destination.Move(moveToDestination.OffsetDifference);
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

        if (botState.DirectionState == BotAction.IDLE)
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
