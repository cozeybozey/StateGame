using Godot;
using System.Collections.Generic;
using System.Drawing;

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

  // Type T unitsGrid because it can be of type Unit[,] or UnitInfo[,] all we do is check for null
  public static List<Vector2I> GetPossibleUnitLocations<T>(T[,] unitsGrid, List<Vector2I> occupiedCells, bool side)
  {
    int startY, endY;
    if (side)
    {
      startY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
      endY = GlobalConstants.GridSize.Y - 1;
    }
    else
    {
      startY = 0;
      endY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
    }

    List<Vector2I> possiblePositions = new();
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = startY; y < endY; y++)
      {
        bool canPlace = true;
        foreach (Vector2I rel in occupiedCells)
        {
          int checkX = x + rel.X;
          int checkY = y + rel.Y;
          if (checkX < 0 || checkY < startY || checkX >= GlobalConstants.GridSize.X || checkY >= endY || unitsGrid[checkX, checkY] != null)
          {
            canPlace = false;
            break;
          }
        }
        if (canPlace)
          possiblePositions.Add(new Vector2I(x, y));
      }
    }

    return possiblePositions;
  }

  public static Vector2I? GetRandomUnitSpawnLocation<T>(T[,] unitsGrid, List<Vector2I> occupiedCells, bool side)
  {
    List<Vector2I> possibleLocations = GetPossibleUnitLocations(unitsGrid, occupiedCells, side);

    if (possibleLocations.Count == 0)
      return null!;


    RandomNumberGenerator _rng = new RandomNumberGenerator();
    _rng.Randomize();
    return possibleLocations[_rng.RandiRange(0, possibleLocations.Count - 1)];
  }
}