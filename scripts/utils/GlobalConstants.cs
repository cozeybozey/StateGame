using Godot;
using System.Collections.Generic;

public static class GlobalConstants
{
  // Tile size in pixels
  public const int TileSize = 32;

  // Combat simulation runs in fixed ticks so it is deterministic and independent of frame rate.
  public const int TicksPerSecond = 20;             // At 1x game speed
  public const int TicksPerTurn = 20;               // A "turn" (turn counter, props, end-of-turn damage) is 1 second at 1x
  public const int TurnsPerBoardUpdate = 5;         // Props act and dead zombies revive once every this many turns
  public const int ActionBarSize = 1000;            // A unit's action bar fills by its Speed every tick; full = act.
                                                    // Speed 10 = 100 ticks = acts every 5 seconds at 1x
  public const int TelegraphTicks = 6;              // Wind-up between targeting and acting (0.3s at 1x)
  public const int MoveTicks = 7;                   // Time a unit takes to travel before it strikes

  // Grid dimensions (columns x rows)
  public static readonly Vector2I GridSize = new Vector2I(16, 16);
  public static readonly Vector2I GridStartPosEnemy = new Vector2I(1, 1); // Starting position of the grid in tile coordinates
  public static readonly Vector2I GridStartPosPlayer = new Vector2I(1, 9); // Starting position of the grid in tile coordinates
  public static Godot.Collections.Dictionary<string, UnitInfo> UnitsData = new Godot.Collections.Dictionary<string, UnitInfo>();
  public static Godot.Collections.Dictionary<string, TerrainInfo> TerrainsData = new Godot.Collections.Dictionary<string, TerrainInfo>();
  public static Godot.Collections.Dictionary<string, PropInfo> PropsData = new Godot.Collections.Dictionary<string, PropInfo>();
  public static readonly Dictionary<string, HashSet<string>> PropTerrainCompatibility = new()
  {
    { "vegetation", new HashSet<string> { "vegetation" } },
    { "burning", new HashSet<string> { "vegetation", "flooring" } },
    { "rocky", new HashSet<string> { "vegetation", "rocky" } },
    { "liquid", new HashSet<string> { "vegetation", "flooring" } },
    { "object", new HashSet<string> { "vegetation", "flooring" } }
  };

}