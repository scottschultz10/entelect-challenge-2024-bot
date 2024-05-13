using SproutReferenceBot.Enums;

internal static class BotServiceHelpers
{
    public static CellType MyTerritory { get; set; }
    public static BotGoal Goal { get; set; }
    public static BotAction LastDirection { get; set; }
    public static CellType MyTrail => (MyTerritory) switch
    {
        CellType.Bot0Territory => CellType.Bot0Trail,
        CellType.Bot1Territory => CellType.Bot1Trail,
        CellType.Bot2Territory => CellType.Bot2Trail,
        CellType.Bot3Territory => CellType.Bot3Trail,
        _ => CellType.Bot0Trail,
    };
}