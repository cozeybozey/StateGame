using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Pusher : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();
    for (int x = 0; x < occupiedMainCell.X; x++)
    {
      Vector2I pos = new(x, occupiedMainCell.Y);
      result.Add(pos);
    }
    return result;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null && targetUnit.occupiedCells.Count == 4)
      {
        int a = 5;
      }
      if (targetUnit != null && targetUnit.occupiedMainCell == target && target.X - 1 >= 0)
      {
        bool canMove = true;
        foreach (Vector2I cell in targetUnit.GetOccupiedCells())
        {
          if (unitsGrid[cell.X - 1, cell.Y] == targetUnit)
            continue;

          if (cell.X - 1 < 0 || unitsGrid[cell.X - 1, cell.Y] != null)
          {
            canMove = false;
            break;
          }
        }

        if (canMove)
          targetUnit.MoveToCell(new Vector2I(target.X - 1, target.Y), playing:true);
      }
    }
  }
}
