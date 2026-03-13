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

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Vector2I? closest = null;
    float closestDist = float.MaxValue;

    if (units == null || units.Count == 0)
      return new List<Vector2I>();

    foreach (Unit unit in units)
    {
      if (unit == null || unit == this)
        continue;

      float dist = occupiedMainCell.DistanceTo(unit.occupiedMainCell);
      if (dist < closestDist)
      {
        closestDist = dist;
        closest = unit.occupiedMainCell;
      }
    }

    if (closest.HasValue)
      return new List<Vector2I> { closest.Value };

    return new List<Vector2I>();
  }
}
