using Godot;

public partial class GlobalFunctions : Node
{
  public static bool IsCellInsideGrid(Vector2I cell)
  {
    return cell.X >= 0 && cell.X < GlobalConstants.GridSize.X && cell.Y >= 0 && cell.Y < GlobalConstants.GridSize.Y;
  }

  public static Vector2I AbsCellToRelCell(Vector2I cell, bool player = true)
  {
    Vector2I subtractedPos = player ? GlobalConstants.GridStartPosPlayer : GlobalConstants.GridStartPosEnemy;
    return cell - subtractedPos;
  }

  public static Vector2I RelCellToAbsCell(Vector2I cell, bool player = true)
  {
    Vector2I addedPos = player ? GlobalConstants.GridStartPosPlayer : GlobalConstants.GridStartPosEnemy;
    return cell + addedPos;
  }

  public static Vector2I CellToGlobalPosition(Vector2I cell)
  {
    return cell * GlobalConstants.TileSize + new Vector2I(Mathf.FloorToInt(0.5 * GlobalConstants.TileSize), Mathf.FloorToInt(0.5 * GlobalConstants.TileSize));
  }
}