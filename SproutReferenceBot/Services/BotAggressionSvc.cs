using SproutReferenceBot.Enums;
using SproutReferenceBot.Globals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Services
{
    public static class BotAggressionSvc
    {

        private static int LeaderboardValue(Dictionary<string, int>? leaderboard)
        {
            return leaderboard?[BotServiceGlobals.BotID.ToString()] ?? 0;
        }

        public static BotAggression BotAggressionLevel(Dictionary<string, int>? leaderboard)
        {
            int leaderboardValue = LeaderboardValue(leaderboard);
            Console.WriteLine($"Leaderboard Value: {leaderboardValue}");
            if (leaderboardValue <= 20)
            {
                return BotAggression.High;
            }
            else if (20 < leaderboardValue && leaderboardValue <= 40)
            {
                return BotAggression.Medium;
            }
            else if (40 < leaderboardValue && leaderboardValue <= 55)
            {
                return BotAggression.Low;
            }
            else
            {
                return BotAggression.None;
            }
        }
    }
}
