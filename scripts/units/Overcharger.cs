using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class Overcharger : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> randomUnits = units
      .Where(u => u.Side == Side && u.Types.Contains("machinery"))
      .OrderBy(_ => GD.Randi())
      .Take(3)
      .Select(u => u.OccupiedMainCell)
      .ToList();
    return randomUnits;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
        targetUnit.ChangeDamage(1);
    }
  }
}
