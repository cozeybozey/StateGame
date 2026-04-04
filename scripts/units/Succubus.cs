using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Succubus : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    int frontDir = Side ? -1 : 1;

    foreach (Vector2I cell in GetOccupiedCells())
    {
      int checkY = cell.Y + frontDir;
      while (GlobalFunctions.IsCellInsideGrid(new Vector2I(cell.X, checkY)))
      {
        Unit unit = unitsGrid[cell.X, checkY];
        if (unit != null)
          return [unit.OccupiedMainCell];
        checkY += frontDir;
      }
    }

    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.Stun();
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

    // Negative score for each unit in front, because this unit stuns the first unit it sees
    foreach (Vector2I cell in unitInfo.OccupiedCells)
    {
      Vector2I newPos = pos + cell;
      int checkY = newPos.Y + 1;
      while (GlobalFunctions.IsCellInsideGrid(new Vector2I(newPos.X, checkY)))
      {
        UnitInfo unit = unitsGrid[newPos.X, checkY];
        if (unit != null)
        {
          score -= 5;
        }
        checkY += 1;
      }
    }

    return score + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
