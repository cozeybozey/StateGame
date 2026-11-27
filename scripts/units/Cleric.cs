using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Cleric : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Every cell of an injured ally that can be seen, so a big ally is healed once for each of its cells
    return units
      .Where(u => u.Side == Side && u.Health < u.MaxHealth)
      .SelectMany(u => u.GetOccupiedCells())
      .Where(cell => LineOfSight.CanSee(GetOccupiedCells(), cell, terrainGrid, propsGrid))
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
