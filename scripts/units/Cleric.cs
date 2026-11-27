using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Cleric : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Every injured ally that can be seen, once
    return units
      .Where(u => u.Side == Side && u.Health < u.MaxHealth && LineOfSight.CanSeeUnit(GetOccupiedCells(), u, terrainGrid, propsGrid))
      .Select(u => u.OccupiedMainCell)
      .ToList();
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null && targetUnit.Health < targetUnit.MaxHealth)
      {
        targetUnit.ChangeHealth(Damage, this);
      }
    }
  }
}
