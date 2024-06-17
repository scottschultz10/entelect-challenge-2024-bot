using SproutReferenceBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Globals
{
    internal static class BotViewGlobals
    {
        public static Dictionary<Location, (BotViewCell Cell, int Tick)> EntireView { get; set; }

        static BotViewGlobals()
        {
            EntireView = [];
        }
    }

}
