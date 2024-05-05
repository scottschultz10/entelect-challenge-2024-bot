using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Enums
{
    public enum BotGoal
    {
        NONE = 0,
        MoveToCorner = 1,
        MoveToLine = 2,
        MoveAlongLine = 3,
        Capturing = 4,
    }
}
