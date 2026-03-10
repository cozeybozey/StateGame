using Godot;
using System;
using System.Collections.Generic;

public partial class Supporter : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeMaxHealth(damage);
      }
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> deadUnits)
  {
    Vector2I? closest = null;
    float closestDist = float.MaxValue;

    for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
    {
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        Unit unit = unitsGrid[x, y];
        if (unit != null && unit != this)
        {
          float dist = occupiedMainCell.DistanceTo(new Vector2I(x, y));
          if (dist < closestDist)
          {
            closestDist = dist;
            closest = new Vector2I(x, y);
          }
        }
      }
    }

    return closest.HasValue ? [closest.Value] : [];
  }
}
