using SproutReferenceBot.Enums;
using SproutReferenceBot.Models;

namespace SproutReferenceBot.Services;
class BotService
{
    private Guid botId;
    private BotStateDTO? lastKnownState;
    private BotStateDTO? botState;
    private bool hasReceivedBotState = false;

    private static readonly int SquareSize = 5;

    private static readonly BotAction[] ActionOrder = new[]
    {
      BotAction.Up,
      BotAction.Right,
      BotAction.Down,
      BotAction.Left,
    };

    private int CurrentAction = 0;
    private int StepsTaken = 0;

    public BotCommand ProcessState()
    {
        if (StepsTaken++ >= SquareSize)
        {
            CurrentAction = (CurrentAction + 1) % ActionOrder.Length;
            StepsTaken = 0;
        }

        var ActionToTake = ActionOrder[CurrentAction];


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
        hasReceivedBotState = true;
    }

    public BotStateDTO? GetBotState()
    {
        return botState;
    }

    public bool HasReceivedBotState()
    {
        return hasReceivedBotState;
    }
}
