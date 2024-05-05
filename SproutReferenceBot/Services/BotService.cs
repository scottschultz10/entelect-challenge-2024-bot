using SproutReferenceBot.Enums;
using SproutReferenceBot.Models;

namespace SproutReferenceBot.Services;
class BotService
{
    private Guid botId;
    private BotStateDTO? botState;
    private readonly BotView botView;
    private CellType myTerritory;
    private bool isCapturing;
    private string? goal;
    private bool hasReceivedBotState = false;
    
    private List<MovementQueueItem> movementQueue;
    private BotViewCell? centerCell;

    public BotService()
    {
        botView = new();
        movementQueue = new();
    }

    public BotCommand ProcessState()
    {
        hasReceivedBotState = false;

        centerCell = botView.GetCenterCell();
        Console.WriteLine($"Center Cell = {centerCell.Location}");
        
        if (movementQueue.Count > 0 && centerCell.CellType == myTerritory && isCapturing)
        {
            //if I am standing in my own territory after capture. Cancel all queued actions and do something new
            movementQueue = new();
        }

        BotCommand? botCommand = CommandFromQueue();
        if (botCommand != null) return botCommand;

        List<BotViewCell> clockwiseView = CellFinderService.GetClockwiseView(botView.Cells);

        //find a corner that matches myTerritory
        CellFinderResult? cornerCell = CellFinderService.FindCornerCell(botView, clockwiseView, myTerritory, botState!.DirectionState);

        Console.WriteLine($"Corner Found = {cornerCell?.Cell.Location}");

        if (cornerCell != null)
        {
            if (cornerCell.Cell.Location == centerCell.Location)
            {
                movementQueue = BotMovementService.CaptureTerritory(cornerCell, botView);
                goal = "Capturing";
                isCapturing = true;
            }
            else
            {
                movementQueue = new() { new(cornerCell.Cell.Location) };
                goal = "Moving to Corner";
                isCapturing = false;
            }

            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        //can't find a corner. Find a line instead
        CellFinderResult? lineCell = CellFinderService.FindLineCell(botView, clockwiseView, myTerritory, botState!.DirectionState);
        Console.WriteLine($"Line Found = {lineCell?.Cell.Location}");

        if (lineCell != null)
        {
            if (lineCell.Cell.Location == centerCell.Location)
            {
                movementQueue = BotMovementService.MoveAlongLine(lineCell, botView, myTerritory);
                goal = "Moving along line";
                isCapturing = false;
            }
            else
            {
                movementQueue = new() { new(lineCell.Cell.Location) };
                goal = "Moving to Line";
                isCapturing = false;
            }

            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        Console.WriteLine("Action from IDLE - IDLE");
        return new()
        {
            BotId = botId,
            Action = BotAction.IDLE,
        };        
    }

    private BotCommand? CommandFromQueue()
    {
        //queue has items. Move along the queue
        if (movementQueue.Count > 0)
        {
            MovementQueueItem thisMovement = movementQueue.First();

            Console.WriteLine($"Movement Queue Destination : {thisMovement.Destination}");

            while (thisMovement.Destination == centerCell!.Location)
            {
                movementQueue.RemoveAt(0);
                if (movementQueue.Count > 0)
                {
                    thisMovement = movementQueue.First();
                }
                else return null;
            }

            Console.WriteLine($"New Destination : {thisMovement.Destination}");
            Console.WriteLine($"Queue = {string.Join("; ", movementQueue.Select(x => x.Destination))} : Current Location: {centerCell!.Location}");

            if (thisMovement.Actions.Count <= 0 || !thisMovement.Actions.AreMovementActionsSafe())
            {
                thisMovement.Actions = BotMovementService.MoveToDestination(centerCell.Location, thisMovement.Destination, botView);
            }

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
        hasReceivedBotState = true;

        if (botState.DirectionState == BotAction.IDLE)
        {
            myTerritory = botView.GetCenterCell().CellType;
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
