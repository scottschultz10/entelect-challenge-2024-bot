using SproutReferenceBot.Enums;
using SproutReferenceBot.Models;

namespace SproutReferenceBot.Services;
class BotService
{
    private Guid botId;
    private BotStateDTO? lastKnownState;
    private BotStateDTO? botState;
    private readonly BotView botView;
    private CellType myTerritory;

    private bool hasReceivedBotState = false;

    private static readonly int squareSize = 5;

    private static readonly BotAction[] actionOrder = new[]
    {
      BotAction.Up,
      BotAction.Right,
      BotAction.Down,
      BotAction.Left,
    };

    private int CurrentAction = 0;
    private int StepsTaken = 0;

    public BotService()
    {
        botView = new();
    }

    public BotCommand ProcessState()
    {
        //find a corner that matches myTerritory
        CellView? cornerCell = CellFinder.FindCornerCell(botView.GetBotView(), CellFinder.GetClockwiseView(botView.GetBotView()), myTerritory);

        if (StepsTaken++ >= squareSize)
        {
            CurrentAction = (CurrentAction + 1) % actionOrder.Length;
            StepsTaken = 0;
        }

        var ActionToTake = actionOrder[CurrentAction];


        hasReceivedBotState = false;
        return new BotCommand
        {
            BotId = botId,
            Action = ActionToTake,
        };
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
            myTerritory = botView.GetBotView()[4][4].CellType;
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
