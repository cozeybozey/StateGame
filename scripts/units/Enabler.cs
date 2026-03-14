using Godot;
using System;
using System.Collections.Generic;

public partial class Enabler : Unit
{
  private int _currentIndex = 0;
  private List<Unit> _units;

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit unit = unitsGrid[target.X, target.Y];

      // Insert the unit after this unit so it immediately gets to play after this unit's turn
      if (unit != null)
      {
        unit.SpawnFloatingText("Extra turn", Colors.Yellow);
        _units.Insert(_currentIndex + 1, unit);
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
      if (unit == null || unit == this || unit is Enabler || unit.side != side)
        continue;

      float dist = occupiedMainCell.DistanceTo(unit.occupiedMainCell);
      if (dist < closestDist)
      {
        closestDist = dist;
        closest = unit.occupiedMainCell;
      }
    }

    if (closest.HasValue)
    {
      _currentIndex = units.IndexOf(this);
      _units = units;
      return new List<Vector2I> { closest.Value };
    }

    return new List<Vector2I>();
  }
}
