using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Booster : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (OccupiedMainCell.X + 1 < GlobalConstants.GridSize.X && unitsGrid[OccupiedMainCell.X + 1, OccupiedMainCell.Y] != null)
      return [new Vector2I(OccupiedMainCell.X + 1, OccupiedMainCell.Y)];
    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
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

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    int score = 0;

    // Everything but the first 3 rows return score of 3,
    // This is to ensure tanky units are only placed in front.
    if (pos.Y <= GlobalConstants.GridSize.Y * 0.5f - 3)
      score += 3;

    // Prefer being next to units that can be boosted.
    if (GlobalFunctions.IsCellInsideGrid(new Vector2I(pos.X + 1, pos.Y)) && unitsGrid[pos.X + 1, pos.Y] != null && unitsGrid[pos.X + 1, pos.Y].Damage > 0)
      score += 5;

    return score;
  }
}
