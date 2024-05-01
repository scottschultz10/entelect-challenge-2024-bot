using SproutReferenceBot.Enums;
using SproutReferenceBot.Models;

namespace SproutReferenceBot.Services;
class BotService
{
    private Guid botId;
    private BotStateDTO? botState;
    private readonly BotView botView;
    private CellType myTerritory;
    private Queue<BotAction> actionQueue;
    private bool isCapturing;
    private string? goal;
    private bool hasReceivedBotState = false;

    public BotService()
    {
        botView = new();
        actionQueue = new();
    }

    public BotCommand ProcessState()
    {
        hasReceivedBotState = false;

        BotViewCell centerCell = botView.GetCenterCell();
        Console.WriteLine($"Center Cell = {centerCell.Location}");
        
        if (actionQueue.Count > 0 && centerCell.CellType == myTerritory && isCapturing)
        {
            //if I am standing in my own territory after capture. Cancel all queued actions and do something new
            actionQueue = new();
        }

        BotCommand? botCommand = CommandFromQueue();
        if (botCommand != null) return botCommand;

        List<BotViewCell> clockwiseView = CellFinder.GetClockwiseView(botView.Cells);

        //find a corner that matches myTerritory
        CellFinderResult? cornerCell = CellFinder.FindCornerCell(botView, clockwiseView, myTerritory, botState!.DirectionState);

        Console.WriteLine($"Corner Found = {cornerCell?.Cell.Location}");

        if (cornerCell != null)
        {
            if (cornerCell.Cell.Location == centerCell.Location)
            {
                actionQueue = BotMovement.CaptureTerritory(cornerCell, botView, myTerritory);
                goal = "Capturing";
                isCapturing = true;
            }
            else
            {
                actionQueue = BotMovement.MoveToDestination(centerCell, cornerCell.Cell, botView);
                goal = "Moving to Corner";
                isCapturing = false;
            }

            botCommand = CommandFromQueue();
            if (botCommand != null) return botCommand;
        }

        //can't find a corner. Find a line instead
        CellFinderResult? lineCell = CellFinder.FindLineCell(botView, clockwiseView, myTerritory, botState!.DirectionState);
        Console.WriteLine($"Line Found = {lineCell?.Cell.Location}");

        if (lineCell != null)
        {
            if (lineCell.Cell.Location == centerCell.Location)
            {
                actionQueue = BotMovement.MoveAlongLine(lineCell, botView, myTerritory);
                goal = "Moving along line";
                isCapturing = false;
            }
            else
            {
                actionQueue = BotMovement.MoveToDestination(centerCell, lineCell.Cell, botView);
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
        if (actionQueue.Count > 0)
        {
            Console.WriteLine($"Goal - {goal}");

            //fix conflicting directions in the queue
            //if (!botState!.DirectionState.CanMove(actionQueue.Peek()))
            //{
            //    Console.WriteLine("Can't move - resetting");
            //    //pop the the item we just checked won't work
            //    actionQueue.Dequeue();

            //    //pop through the queue to find the next direction that will work and reset the queue for the next tick
            //    while(actionQueue.Count > 0)
            //    {
            //        BotAction action = actionQueue.Dequeue();
            //        if (botState.DirectionState.CanMove(action))
            //        {
            //            actionQueue = new();
            //            return new()
            //            {
            //                BotId = botId,
            //                Action = action
            //            };
            //        }
            //    }

            //    //couldn't find a direction - just move clockwise 
            //    actionQueue = new();
            //    return new()
            //    {
            //        BotId = botId,
            //        Action = botState.DirectionState.ToLocationDirection().NextClockwiseDirection().ToBotAction(),
            //    };
            //}

            BotCommand command = new()
            {
                BotId = botId,
                Action = actionQueue.Dequeue(),
            };

            return command;
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
