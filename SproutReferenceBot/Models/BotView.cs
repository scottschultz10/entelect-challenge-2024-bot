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
        private List<List<BotViewCell>> cells;
        public List<List<BotViewCell>> Cells { get { return cells; } }

        public BotView()
        {
            cells = new();
        }

        public void SetBotView(BotStateDTO botState)
        {
            cells = new();

            //my bot will always be in 4, 4. So populate X,Y based on that
            int botX = botState.X;
            int botY = botState.Y;

            for (int x = 0; x < botState.HeroWindow?.Count; x++)
            {
                cells.Add(new());
                for (int y = 0; y < botState.HeroWindow[x].Count; y++)
                {
                    int cellY = botY + (y - 4);
                    int cellX = botX + (x - 4);

                    bool hasBot = botState.BotPostions?.Any(x => x == new Location(cellX, cellY)) ?? false;
                    bool isMe = (botX == cellX && botY == cellY);

                    bool hasWeed = botState.Weeds?[x][y] ?? false;

                    CellType cellType = botState.HeroWindow[x][y];

                    PowerUpType powerUpType = botState.PowerUpLocations?.FirstOrDefault(x => x.Location == new Location(cellX, cellY))?.Type ?? PowerUpType.NONE;

                    cells[x].Add(new()
                    {
                        Location = new(cellX, cellY),
                        IsMe = isMe,
                        HasBot = hasBot && !isMe,
                        HasWeed = hasWeed,
                        CellType = cellType,
                        PowerUpType = powerUpType
                    });
                }
            }
        }

        public BotViewCell CenterCell()
        {
            return cells[(cells.Count - 1) / 2][(cells[0].Count - 1) / 2];
        }

        public List<BotViewCell> CenterCellConeView(Location direction)
        {
            BotViewCell centerCell = CenterCell();

            Location fromQuadrant = direction.NextCounterClockwiseQuadrant();
            Location toQuadrant = direction.NextClockwiseQuadrant();
            Location populationDirection = direction.NextClockwiseDirection();

            Location fromLocation = centerCell.Location.Move(fromQuadrant);
            Location toLocation = centerCell.Location.Move(toQuadrant);

            List<BotViewCell> returnView = new();
            while (CellByLocation(fromLocation) != null || CellByLocation(toLocation) != null)
            {
                Location currentLocation = fromLocation;
                BotViewCell? fromCell = CellByLocation(fromLocation);
                if (fromCell != null)
                {
                    returnView.Add(fromCell);
                }

                while (currentLocation != toLocation)
                {
                    currentLocation = currentLocation.Move(populationDirection);
                    BotViewCell? currentCell = CellByLocation(currentLocation);

                    if (currentCell != null)
                    {
                        returnView.Add(currentCell);
                    }
                }

                fromLocation = fromLocation.Move(fromQuadrant);
                toLocation = toLocation.Move(toQuadrant);
            }

            return returnView.OrderBy(x => centerCell.Location.DistanceTo(x.Location)).ThenBy(x => (centerCell.Location.CommonDirection(x.Location) == direction) ? -1 : 1).ToList();
        }

        /// <summary>
        /// Get all cells in a straight line in the given direction from the center cell within the bot view
        /// </summary>
        public List<BotViewCell> CenterCellDirectionView(Location direction)
        {
            Location currentLocation = CenterCell().Location.Move(direction);

            List<BotViewCell> returnView = new();
            while(CellByLocation(currentLocation) != null)
            {
                returnView.Add(CellByLocation(currentLocation)!);
                currentLocation = currentLocation.Move(direction);
            }

            return returnView;
        }

        public List<BotViewCell> CellBufferByLocation(Location location)
        {
            List<Location> allBuffers = new()
            {
                LocationQuadrant.East,
                LocationQuadrant.South,
                LocationQuadrant.West,
                LocationQuadrant.North,
                LocationQuadrant.NorthEast,
                LocationQuadrant.NorthWest,
                LocationQuadrant.SouthEast,
                LocationQuadrant.SouthWest,
                LocationQuadrant.NONE,
            };

            List<BotViewCell> returnList = new();
            foreach (Location buffer in allBuffers)
            {
                BotViewCell? bufferCell = CellByLocation(location.Move(buffer));
                if (bufferCell != null)
                {
                    returnList.Add(bufferCell);
                }
            }

            return returnList;
        }

        /// <summary>
        /// Buffer that only contains the Right, Down, Left, Up values 
        /// </summary>
        /// <returns></returns>
        public List<BotViewCell> CellPrimaryBufferByLocation(Location location)
        {
            List<Location> allBuffers = new()
            {
                LocationQuadrant.East,
                LocationQuadrant.South,
                LocationQuadrant.West,
                LocationQuadrant.North,
            };

            List<BotViewCell> returnList = new();
            foreach (Location buffer in allBuffers)
            {
                BotViewCell? bufferCell = CellByLocation(location.Move(buffer));
                if (bufferCell != null)
                {
                    returnList.Add(bufferCell);
                }
            }

            return returnList;
        }

        /// <summary>
        /// Send in a location value and find the associated cell in the BotView
        /// </summary>
        /// <param name="location"></param>
        /// <returns>The BotViewCell associated with the sent in location. If no location is found return null</returns>
        public BotViewCell? CellByLocation(Location location)
        {
            if (cells.Count <=  0 || cells[^1].Count <= 0)
            {
                return null;
            }

            //get the top-left cell and bottom-right cell as a reference / validation
            BotViewCell topLeft = cells[0][0];
            BotViewCell bottomRight = cells[cells.Count - 1][cells[0].Count - 1];

            //so if the location has any X / Y value lower than the topLeft
            // or the location has any X / Y value higher than the bottomRight
            if (topLeft.Location.X > location.X || location.X > bottomRight.Location.X
                || topLeft.Location.Y > location.Y || location.Y > bottomRight.Location.Y)
            {
                return null;
            }

            //find how far away location is from 0,0
            //the difference is the index in the cells list
            Location index = location.Difference(topLeft.Location);
            
            if (index.X > cells.Count || index.Y > cells[^1].Count)
            {
                return null;
            }

            return cells[index.X][index.Y];
        }

        public string PrintBotView()
        {
            StringBuilder builder = new();

            if (cells.Count > 0)
            {
                for (int x = 0; x < cells.Count; x++)
                {
                    List<BotViewCell> viewRow = cells[x];

                    string locations = AddLineToTable(viewRow.Select(y => $"{y.Location},{(y.IsMe ? " <- ME" : string.Empty)}").ToList());
                    string cellTypes = AddLineToTable(viewRow.Select(y => y.CellType.ToString()).ToList());
                    string extras = AddLineToTable(viewRow.Select(y => ExtrasString(y.HasBot, y.HasWeed, y.PowerUpType)).ToList());

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

}
