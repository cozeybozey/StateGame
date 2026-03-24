using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Booster : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (OccupiedMainCell.X + 1 < GlobalConstants.GridSize.X && unitsGrid[OccupiedMainCell.X + 1, OccupiedMainCell.Y] != null)
      return [new Vector2I(OccupiedMainCell.X + 1, OccupiedMainCell.Y)];
    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeDamage(targetUnit.Damage);
      }
    }
  }
}
