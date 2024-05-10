using SproutReferenceBot.Enums;
using SproutReferenceBot.Models;

namespace SproutReferenceBot.Services;
class BotService
{
    private Guid botId;
    private BotStateDTO? botState;
    private readonly BotView botView;
    private List<BotViewCell> clockwiseView;

    private CellType myTerritory;
    private BotGoal goal;
    private bool hasReceivedBotState = false;
    
    private List<MovementQueueItem> movementQueue;
    private BotViewCell? centerCell;

    private Location? startingLocation;

    public BotService()
    {
        botView = new();
        clockwiseView = new();
        movementQueue = new();
    }

    public BotCommand ProcessState()
    {
        hasReceivedBotState = false;

        if (botState!.DirectionState == BotAction.IDLE)
        {
            //bot has been reset. Clear everything
            movementQueue = new();
            goal = BotGoal.NONE;
        }

        centerCell = botView.GetCenterCell();
        Console.WriteLine($"Center Cell = {centerCell.Location}");

        if (movementQueue.Count > 0 && centerCell.CellType == myTerritory && goal == BotGoal.Capture)
        {
            //if I am standing in my own territory after capture. Cancel all queued actions and do something new
            movementQueue = new();
        }

        BotCommand? botCommand;
        if (goal != BotGoal.MoveAlongLine && goal != BotGoal.MoveToLine)
        {
            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        //find a corner that matches myTerritory
        CellFinderResult? cornerCell = CellFinderService.FindCornerCell(botView, clockwiseView, myTerritory, botState!.DirectionState);
        CellFinderResult? lineCell = CellFinderService.FindLineCell(botView, clockwiseView, myTerritory, botState!.DirectionState);

        Console.WriteLine($"Corner Found = {cornerCell?.Cell.Location}");
        Console.WriteLine($"Line Found = {lineCell?.Cell.Location}");

        if (cornerCell != null && cornerCell.Cell.Location == centerCell.Location)
        {
            movementQueue = BotMovementService.CaptureTerritory(cornerCell, botView, myTerritory);
            goal = BotGoal.Capture;

            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        if (lineCell != null && lineCell.Cell.Location == centerCell.Location && lineCell.CanCapture)
        {
            movementQueue = BotMovementService.CaptureTerritory(lineCell, botView, myTerritory);
            goal = BotGoal.Capture;

            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        if (cornerCell != null)
        {
            if (cornerCell.HasPriority || lineCell == null)
            {
                movementQueue = new() { new(cornerCell.Cell.Location, new(centerCell.Location.CommonDirection(cornerCell.Cell.Location), centerCell.Location.RotationFromDestination(cornerCell.Cell.Location))) };
                goal = BotGoal.MoveToCorner;

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
                    movementQueue = BotMovementService.CaptureTerritory(lineCell, botView, myTerritory);
                    goal = BotGoal.Capture;
                }
                else
                {
                    movementQueue = BotMovementService.MoveAlongLine(lineCell, botView, myTerritory);
                    goal = BotGoal.MoveAlongLine;
                }
            }
            else
            {
                movementQueue = new() { new(lineCell.Cell.Location, lineCell.Directions.First()) };
                goal = BotGoal.MoveToLine;
            }
        }

        botCommand = CommandFromQueue();
        if (botCommand != null) return botCommand;

        //return to starting location to try and find something
        movementQueue = new() { new(startingLocation!, new (centerCell.Location.CommonDirection(startingLocation!), centerCell.Location.RotationFromDestination(startingLocation!))) };

        return CommandFromQueue()!;
    }

    private BotCommand? CommandFromQueue()
    {
        //queue has items. Move along the queue
        if (movementQueue.Count > 0)
        {
            MovementQueueItem thisMovement = movementQueue.First();

            while (thisMovement.Destination == centerCell!.Location)
            {
                movementQueue.RemoveAt(0);
                if (movementQueue.Count > 0)
                {
                    thisMovement = movementQueue.First();
                }
                else return null;
            }

            if (thisMovement.Actions.Count <= 0 || !thisMovement.Actions.AreMovementActionsSafe(botView))
            {
                thisMovement.Actions = BotMovementService.MoveToDestination(centerCell.Location, thisMovement.Destination, botView, botState!.DirectionState, thisMovement.Direction.Rotation, myTerritory, goal, LocationDirection.NONE);
                thisMovement.Destination = thisMovement.Actions.Last().Location;
            }
            
            Console.WriteLine($"Movement Destination : {thisMovement.Destination}");
            Console.WriteLine($"Queue = {string.Join("; ", movementQueue.Select(x => x.Destination))} : Current Location: {centerCell!.Location}");

            if (thisMovement.Actions.Count > 0)
            {
                //keep going through the queue
                BotAction botAction = thisMovement.Actions.First().Action;
                thisMovement.Actions.RemoveAt(0);

                Console.WriteLine($"Goal - {goal}");
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
        hasReceivedBotState = true;

        if (botState.DirectionState == BotAction.IDLE)
        {
            myTerritory = botView.GetCenterCell().CellType;
            startingLocation = botView.GetCenterCell().Location;
        }
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
