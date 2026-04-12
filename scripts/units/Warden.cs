using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

public partial class Warden : Unit
{

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets.Count == 0) return;

    Unit target = unitsGrid[targets[0].X, targets[0].Y];
    if (target == null) return;

    int holyBonus = 0;
    // Count friendly holy units in the same rows as this unit
    List<Unit> countedUnits = new List<Unit>();
    foreach (Vector2I cell in GetOccupiedCells())
    {
      // check all cells in this row
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        Unit unit = unitsGrid[x, cell.Y];
        if (unit != null && unit != this && unit.Side == Side && unit.Types.Contains("holy") && !countedUnits.Contains(unit))
        {
          holyBonus++;
          countedUnits.Add(unit);
        }
      }
    }

    target.ChangeHealth(-Damage - holyBonus, this);
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    int score = 0;

    // Everything but the first 3 rows return score of 3,
    // This is to ensure tanky units are only placed in front.
    if (pos.Y <= GlobalConstants.GridSize.Y * 0.5f - 3)
      score += 3;

    // Count friendly holy units in the same rows as this unit and gain score for each one
    List<UnitInfo> countedUnits = new List<UnitInfo>();
    foreach (Vector2I cell in unitInfo.OccupiedCells)
    {
      Vector2I newPos = pos + cell;

      // check all cells in this row
      for (int x = 0; x < GlobalConstants.GridSize.X; x++)
      {
        UnitInfo unit = unitsGrid[x, newPos.Y];
        if (unit != null && unit.Types.Contains("holy") && !countedUnits.Contains(unit))
        {
          score++;
          countedUnits.Add(unit);
        }
      }
    }

    return score + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
