using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Healer : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // The frontmost injured ally that can be seen
    Unit? target = units
      .Where(u => u.Side == Side && u.Health < u.MaxHealth && LineOfSight.CanSeeUnit(GetOccupiedCells(), u, terrainGrid, propsGrid))
      .OrderBy(u => Side ? u.OccupiedMainCell.Y : -u.OccupiedMainCell.Y)
      .ThenBy(u => u.OccupiedMainCell.X)
      .FirstOrDefault();
    return target == null ? [] : [target.OccupiedMainCell];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(Damage, this);
      }
    }
  }
}
