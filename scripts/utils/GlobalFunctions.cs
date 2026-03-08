using Godot;
using System.Collections.Generic;

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

  public static Vector2I CellToGlobalPosition(Vector2I cell, int width, int height, Vector2I relPos)
  {
    Vector2I globalPos = cell * GlobalConstants.TileSize + new Vector2I(Mathf.FloorToInt((0.5 * width - relPos.X) * GlobalConstants.TileSize), Mathf.FloorToInt((0.5 * height - relPos.Y) * GlobalConstants.TileSize));
    return globalPos;
  }

  public static Vector2I CellsToDimensions(List<Vector2I> cells)
  {
    int minX = cells[0].X;
    int maxX = cells[0].X;
    int minY = cells[0].X;
    int maxY = cells[0].X;
    foreach (Vector2I cell in cells)
    {
      if (cell.X < minX)
        minX = cell.X;
      if (cell.X > maxX)
        maxX = cell.X;
      if (cell.Y < minY)
        minY = cell.Y;
      if (cell.Y > maxY)
        maxY = cell.Y;
    }

    return new Vector2I(maxX - minX + 1, maxY - minY + 1);
  }

  public static Vector2I GetRelPosInCells(List<Vector2I> cells, Vector2I targetCell)
  {
    int minX = cells[0].X;
    int minY = cells[0].X;
    foreach (Vector2I cell in cells)
    {
      if (cell.X < minX)
        minX = cell.X;
      if (cell.Y < minY)
        minY = cell.Y;
    }

    return new Vector2I(targetCell.X - minX, targetCell.Y - minY);
  }
}