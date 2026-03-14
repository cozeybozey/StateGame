using Godot;

public static class GlobalConstants
{
    // Tile size in pixels
    public const int TileSize = 32;

    // Grid dimensions (columns x rows)
    public static readonly Vector2I GridSize = new Vector2I(16, 16);
    public static readonly Vector2I GridStartPosEnemy = new Vector2I(1, 1); // Starting position of the grid in tile coordinates
    public static readonly Vector2I GridStartPosPlayer = new Vector2I(1, 9); // Starting position of the grid in tile coordinates
    public static Godot.Collections.Dictionary<string, UnitInfo> UnitsData = new Godot.Collections.Dictionary<string, UnitInfo>();
}