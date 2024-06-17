using SproutReferenceBot.Enums;
using SproutReferenceBot.Globals;
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
        public List<List<BotViewCell>> Cells => cells;

        /// <summary>
        /// Expanded clockwise view. Adds 2 extra cells of not seen cells as extras
        /// </summary>
        private List<BotViewCell> clockwiseView;
        public List<BotViewCell> ClockwiseView => clockwiseView;

        public BotView()
        {
            cells ??= [];
            clockwiseView ??= [];
        }

        public BotView(BotStateDTO botState) : this()
        {
            SetBotView(botState);
            //SetClockwiseView();
            SetExpandedClockwiseView();
        }

        private void SetBotView(BotStateDTO botState)
        {
            cells = [];

            //my bot will always be in 4, 4. So populate X,Y based on that
            int botX = botState.X;
            int botY = botState.Y;

            for (int x = 0; x < botState.HeroWindow?.Count; x++)
            {
                cells.Add([]);
                for (int y = 0; y < botState.HeroWindow[x].Count; y++)
                {
                    int cellY = botY + (y - 4);
                    int cellX = botX + (x - 4);

                    Location cellLocation = new(cellX, cellY);

                    bool hasBot = botState.BotPostions?.Any(x => x == cellLocation) ?? false;
                    bool isMe = (botX == cellX && botY == cellY);

                    bool hasWeed = botState.Weeds?[x][y] ?? false;

                    CellType cellType = botState.HeroWindow[x][y];

                    PowerUpType powerUpType = botState.PowerUpLocations?.FirstOrDefault(x => (x.Location ?? LocationDirection.NONE) == cellLocation)?.Type ?? PowerUpType.NONE;

                    BotViewCell cell = new()
                    {
                        Location = new(cellX, cellY),
                        IsMe = isMe,
                        HasBot = hasBot && !isMe,
                        HasWeed = hasWeed,
                        CellType = cellType,
                        PowerUpType = powerUpType
                    };

                    cells[x].Add(cell);

                    //add / update the cell into the whole map view
                    if (!BotViewGlobals.EntireView.TryAdd(cellLocation, (cell, botState.GameTick)))
                    {
                        BotViewGlobals.EntireView[cellLocation] = (cell, botState.GameTick);
                    }
                }
            }
        }

        [Obsolete]
        /// <summary>
        /// Build a 1 dimensional list of cellviews starting from the bot and spiralling outwards
        /// </summary>
        /// <returns>A 1 dimensional list of cellviews that gradually get further from the bot as the list continues</returns>
        private void SetClockwiseView()
        {
            if (cells.Count == 0) return;

            BotViewCell centerCell = CenterCell();

            List<BotViewCell> clockwiseView = [centerCell];

            BotViewCell? currentCell = CellByLocation(centerCell.Location.Move(LocationDirection.Right));
            Location currentDirection = LocationDirection.Down;
            int directionMax = 1;
            int directionCount = 0;

            while (currentCell != null)
            {
                clockwiseView.Add(currentCell);

                if (directionCount >= directionMax)
                {
                    //reset the count
                    directionCount = 0;

                    //change the direction
                    currentDirection = currentDirection.NextClockwiseDirection();

                    //increment the direction max on left and right
                    if (currentDirection == LocationDirection.Left || currentDirection == LocationDirection.Right)
                    {
                        directionMax++;
                    }
                }

                directionCount++;
                currentCell = CellByLocation(currentCell.Location.Move(currentDirection));
            }

            this.clockwiseView = clockwiseView;
        }

        private void SetExpandedClockwiseView()
        {
            if (BotViewGlobals.EntireView.Count == 0) return;

            BotViewCell centerCell = CenterCell();

            List<BotViewCell> clockwiseView = [centerCell];

            Location currentLocation = centerCell.Location.Move(LocationDirection.Right);
            Location currentDirection = LocationDirection.Down;
            int directionMax = 1;
            int directionCount = 0;

            //set to be 2 more spaces than normal bot view. 6 spaces in each direction
            for (int i = 0; i <= 169; i++)
            {
                BotViewCell? currentCell = CellByLocation(currentLocation);

                if (currentCell != null)
                {
                    clockwiseView.Add(currentCell);
                }

                if (directionCount >= directionMax)
                {
                    //reset the count
                    directionCount = 0;

                    //change the direction
                    currentDirection = currentDirection.NextClockwiseDirection();

                    //increment the direction max on left and right
                    if (currentDirection == LocationDirection.Left || currentDirection == LocationDirection.Right)
                    {
                        directionMax++;
                    }
                }

                directionCount++;
                currentLocation = currentLocation.Move(currentDirection);
            }

            this.clockwiseView = clockwiseView;
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

            List<BotViewCell> returnView = [];
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

            return [.. returnView.OrderBy(x => centerCell.Location.DistanceTo(x.Location)).ThenBy(x => (centerCell.Location.CommonDirection(x.Location) == direction) ? -1 : 1)];
        }

        /// <summary>
        /// Get all cells in a straight line in the given direction from the center cell within the bot view
        /// </summary>
        public List<BotViewCell> CenterCellDirectionView(Location direction)
        {
            Location currentLocation = CenterCell().Location.Move(direction);

            List<BotViewCell> returnView = [];
            while (CellByLocation(currentLocation) != null)
            {
                returnView.Add(CellByLocation(currentLocation)!);
                currentLocation = currentLocation.Move(direction);
            }

            return returnView;
        }

        public List<BotViewCell> CellBufferByLocation(Location location)
        {
            List<Location> allBuffers =
            [
                LocationQuadrant.East,
                LocationQuadrant.South,
                LocationQuadrant.West,
                LocationQuadrant.North,
                LocationQuadrant.NorthEast,
                LocationQuadrant.NorthWest,
                LocationQuadrant.SouthEast,
                LocationQuadrant.SouthWest,
                LocationQuadrant.NONE,
            ];

            List<BotViewCell> returnList = [];
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
            List<Location> allBuffers =
            [
                LocationQuadrant.East,
                LocationQuadrant.South,
                LocationQuadrant.West,
                LocationQuadrant.North,
            ];

            List<BotViewCell> returnList = [];
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
            if (BotViewGlobals.EntireView.TryGetValue(location, out (BotViewCell Cell, int Tick) cell))
            {
                //check the age of the cell. Do not return old values
                if (BotServiceGlobals.GameTick <= (cell.Tick + 20))
                {
                    return cell.Cell;
                }
                else return null;
            }
            else return null;
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

                    string border = new('-', (new[] { locations.Length, cellTypes.Length, extras.Length }).Max());

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
            List<string> extras = [];
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

            if (extras.Count > 0)
            {
                return string.Join(", ", extras);
            }
            else return "-";
        }
    }

}
