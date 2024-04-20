using SproutReferenceBot.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SproutReferenceBot.Models
{
    /// <summary>
    /// Combine all elements that the bot can see into one matrix
    /// </summary>
    public class BotView
    {
        private List<List<CellView>> cellViews;

        public BotView()
        {
            cellViews = new();
        }

        public void SetBotView(BotStateDTO botState)
        {
            cellViews = new();

            //my bot will always be in 4, 4. So populate X,Y based on that
            int botX = botState.X;
            int botY = botState.Y;

            for (int y = 0; y < botState.HeroWindow?.Count; y++)
            {
                cellViews.Add(new());
                for (int x = 0; x < botState.HeroWindow[y].Count; x++)
                {
                    int cellY = botY + (y - 4);
                    int cellX = botX + (x - 4);

                    bool hasBot = botState.BotPostions?.Any(x => x.Equals(cellX, cellY)) ?? false;
                    bool isMe = (botX == cellX && botY == cellY);

                    bool hasWeed = botState.Weeds?[x][y] ?? false;

                    CellType cellType = botState.HeroWindow[x][y];

                    PowerUpType powerUpType = botState.PowerUpLocations?.FirstOrDefault(x => x.Location?.Equals(cellX, cellY) ?? false)?.Type ?? PowerUpType.NONE;

                    cellViews[y].Add(new()
                    {
                        X = cellX,
                        Y = cellY,
                        IsMe = isMe,
                        HasBot = hasBot && !isMe,
                        HasWeed = hasWeed,
                        CellType = cellType,
                        PowerUpType = powerUpType
                    });
                }
            }
        }

        public List<List<CellView>> GetBotView()
        {
            return cellViews;
        }

        public string PrintBotView()
        {
            StringBuilder builder = new();

            if (cellViews.Any())
            {
                for (int y = 0; y < cellViews.Count; y++)
                {
                    List<CellView> viewRow = cellViews[y];

                    string locations = AddLineToTable(viewRow.Select(x => $"({x.X}, {x.Y}){(x.IsMe ? " <- ME" : string.Empty)}").ToList());
                    string cellTypes = AddLineToTable(viewRow.Select(x => x.CellType.ToString()).ToList());
                    string extras = AddLineToTable(viewRow.Select(x => ExtrasString(x.HasBot, x.HasWeed, x.PowerUpType)).ToList());

                    string border = new string('-', (new[] { locations.Length, cellTypes.Length, extras.Length }).Max());

                    builder.AppendJoin('\n', locations, cellTypes, extras, border);
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string AddLineToTable(List<string> items)
        {
            string line = "";
            foreach (string item in items)
            {
                line += item.PadRight(15);
            }
            return line;
        }

        private static string ExtrasString(bool hasBot, bool hasWeed, PowerUpType powerUpType)
        {
            List<string> extras = new();
            if (hasBot)
            {
                extras.Add("Bot");
            }
            if (hasWeed)
            {
                extras.Add("Weed");
            }
            if (powerUpType > 0)
            {
                extras.Add(powerUpType.ToString());
            }

            if (extras.Any())
            {
                return string.Join(", ", extras);
            }
            else return "-";
        }
    }

    public class CellView
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool HasBot { get; set; }
        public bool IsMe { get; set; }
        public bool HasWeed { get; set; }
        public CellType CellType { get; set; }
        public PowerUpType PowerUpType { get; set; }
    }
}
