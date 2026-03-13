using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Mover : Unit
{
  private bool _movingRight = true;

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    // Buff surrounding units
    List<Unit> buffed = new List<Unit>();
    foreach (Vector2I cell in targets)
    {
      Unit unit = unitsGrid[cell.X, cell.Y];
      if (unit == null || unit == this)
        continue;
      if (buffed.Contains(unit))
        continue;
      buffed.Add(unit);
      unit.ChangeDamage(1);
      unit.ChangeMaxHealth(1);
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new List<Vector2I>();

    int dir = _movingRight ? 1 : -1;
    bool moved = false;

    // Check primary direction
    if (CanPlaceAtOffset(dir, unitsGrid))
    {
      moved = true;
      MoveToCell(occupiedMainCell + new Vector2I(dir, 0), playing: true);
    }
    // Otherwise suggest opposite direction if possible (but do not change state)
    else if (CanPlaceAtOffset(-dir, unitsGrid))
    {
      moved = true;
      MoveToCell(occupiedMainCell + new Vector2I(-dir, 0), playing: true);
    }

    if (!moved)
      return [];

    // Return surrounding cells
    for (int x = - 1; x <= 1; x++)
    {
      for (int y = -1; y <= 1; y++)
      {
        Vector2I cell = new Vector2I(x, y);
        if (cell != new Vector2I(0, 0) && GlobalFunctions.IsCellInsideGrid(occupiedMainCell + cell))
          result.Add(occupiedMainCell + cell);
      }
    }

    return result;
  }

  private bool CanPlaceAtOffset(int xOffset, Unit[,] unitsGrid)
  {
    Vector2I newMain = occupiedMainCell + new Vector2I(xOffset, 0);
    foreach (Vector2I rel in occupiedCells)
    {
      int nx = newMain.X + rel.X;
      int ny = newMain.Y + rel.Y;
      if (!GlobalFunctions.IsCellInsideGrid(new Vector2I(nx, ny)))
        return false;
      Unit unit = unitsGrid[nx, ny];
      if (unit != null && unit != this)
        return false;
    }
    return true;
  }
}
