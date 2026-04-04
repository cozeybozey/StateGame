using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class BeaconOfLight : Unit
{

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

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    return GetSurroundingCells(includeDiagonals: true);
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    int score = 0;

    // Everything but the first 3 rows return score of 3,
    // This is to ensure tanky units are only placed in front.
    if (pos.Y <= GlobalConstants.GridSize.Y * 0.5f - 3)
      score += 3;

    // Favor placements that can heal more units
    foreach (Vector2I cell in GlobalFunctions.GetSurroundingCells(pos, unitInfo.OccupiedCells, false, includeDiagonals: true))
    {
      if (unitsGrid[cell.X, cell.Y] != null)
        score += 5; 
    }

    return score;
  }
}
