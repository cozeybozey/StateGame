using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Booster : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (occupiedMainCell.X + 1 < GlobalConstants.GridSize.X && unitsGrid[occupiedMainCell.X + 1, occupiedMainCell.Y] != null)
      return [new Vector2I(occupiedMainCell.X + 1, occupiedMainCell.Y)];
    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeDamage(targetUnit.damage);
      }
    }
  }
}
