using Godot;

public partial class GlobalFunctions : Node
{
  public static bool IsCellInsideGrid(Vector2I cell)
  {
    Vector2I relCell = AbsCellToRelCell(cell);
    return relCell.X >= 0 && relCell.X < GlobalConstants.GridSize.X && relCell.Y >= 0 && relCell.Y < GlobalConstants.GridSize.Y;
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

  public static Vector2I RelCellToGlobalPosition(Vector2I cell, bool player = true)
  {
    Vector2I relCel = player ? cell + GlobalConstants.GridStartPosPlayer : cell + GlobalConstants.GridStartPosEnemy;
    return relCel * GlobalConstants.TileSize + new Vector2I(Mathf.FloorToInt(0.5 * GlobalConstants.TileSize), Mathf.FloorToInt(0.5 * GlobalConstants.TileSize));
  }

  public static Vector2I GlobalPositionToAbsCell(Vector2 position, bool player = true)
  {
    Vector2I cell = new Vector2I(Mathf.FloorToInt(position.X / GlobalConstants.TileSize), Mathf.FloorToInt(position.Y / GlobalConstants.TileSize));
    return cell;
  }
}