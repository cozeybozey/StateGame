using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Saboteur : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();
    for (int dx = -2; dx <= 2; dx++)
    {
      for (int dy = -2; dy <= 2; dy++)
      {
        int distance = Mathf.Abs(dx) + Mathf.Abs(dy);

        if (distance == 0 || distance > 2)
          continue;

        Vector2I pos = new(occupiedMainCell.X + dx, occupiedMainCell.Y + dy);

        if (!GlobalFunctions.IsCellInsideGrid(pos))
          continue;

        result.Add(pos);
      }
    }

    if (side)
    {
      if (occupiedMainCell.Y - 1 < 0 || unitsGrid[occupiedMainCell.X, occupiedMainCell.Y - 1] != null)
      {
        return result;
      }
    }
    else
    {
      if (occupiedMainCell.Y + 1 >= GlobalConstants.GridSize.Y || unitsGrid[occupiedMainCell.X, occupiedMainCell.Y + 1] != null)
      {
        return result;
      }
    }
    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    if (targets.Count == 0)
    {
      if (side)
      {
        MoveToCell(new Vector2I(occupiedMainCell.X, occupiedMainCell.Y - 1), playing:true);
      }
      else
      {
        MoveToCell(new Vector2I(occupiedMainCell.X, occupiedMainCell.Y + 1), playing:true);
      }
    }
    else
    {
      foreach (Vector2I target in targets)
      {
        Unit targetUnit = unitsGrid[target.X, target.Y];
        if (targetUnit != null)
        {
          targetUnit.ChangeHealth(-damage, this);
        }
      }
      ChangeHealth(-health, this); // Sacrifice self
    }
  }
}
