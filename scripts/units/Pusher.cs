using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Pusher : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();
    for (int x = 0; x < OccupiedMainCell.X; x++)
    {
      Vector2I pos = new(x, OccupiedMainCell.Y);
      result.Add(pos);
    }
    return result;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid)
  {
    List<Unit> movedUnits = new List<Unit>();

    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null && target.X - 1 >= 0 && !movedUnits.Contains(targetUnit))
      {
        bool canMove = true;
        foreach (Vector2I cell in targetUnit.GetOccupiedCells())
        {
          if (cell.X - 1 >= 0 && unitsGrid[cell.X - 1, cell.Y] == targetUnit)
            continue;

          if (cell.X - 1 < 0 || unitsGrid[cell.X - 1, cell.Y] != null ||
            (terrainGrid[cell.X - 1, cell.Y] != null && terrainGrid[cell.X - 1, cell.Y].Blocking))
          {
            canMove = false;
            break;
          }
        }

        if (canMove)
        {
          targetUnit.MoveToCell(targetUnit.OccupiedMainCell + new Vector2I(-1, 0), playing: true);
          movedUnits.Add(targetUnit);
        }
      }
    }
  }
}
