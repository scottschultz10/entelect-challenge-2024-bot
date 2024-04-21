using System.Collections;
using System.Text;
using SproutReferenceBot.Enums;

namespace SproutReferenceBot.Models;

public class PowerUpLocation
{
    public Location? Location { get; set; }
    public PowerUpType Type { get; set; }
}

public class BotStateDTO
{
    public int X { get; set; }
    public int Y { get; set; }
    public string? ConnectionId { get; set; }
    public string? ElapsedTime { get; set; }
    public int GameTick { get; set; }
    public List<List<CellType>>? HeroWindow { get; set; }
    public BotAction DirectionState { get; set; }
    /// <summary>
    /// Bot Nickname, territory percentage
    /// </summary>
    public Dictionary<string, int>? LeaderBoard { get; set; }
    public List<Location>? BotPostions { get; set; }
    public List<PowerUpLocation>? PowerUpLocations { get; set; }
    public List<List<bool>>? Weeds { get; set; }
    public PowerUpType PowerUp { get; set; }
    public SuperPowerUpType SuperPowerUp { get; set; }

    public string PrintBotState()
    {
        StringBuilder builder = new();

        builder.AppendLine($"Bot Position = (x: {X}, y: {Y})");
        builder.AppendLine($"GameTick = {GameTick}");
        builder.AppendLine($"DirectionState = {DirectionState}");
        
        builder.AppendLine("Leaderboard: \n");

        if (LeaderBoard != null)
        {
            foreach (var bot in LeaderBoard)
            {
                builder.AppendLine($"- {bot.Key}, {bot.Value}");
            }
        }

        return builder.ToString();
    }
}