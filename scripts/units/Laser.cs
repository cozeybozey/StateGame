using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Laser : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> cells = new();

    if (Side)
    {
      for (int y = OccupiedMainCell.Y - 1; y >= 0; y--)
      {
        cells.Add(new Vector2I(OccupiedMainCell.X, y));
        cells.Add(new Vector2I(OccupiedMainCell.X + 1, y));
      }
    }
    else
    {
      for (int y = OccupiedMainCell.Y + 1; y < GlobalConstants.GridSize.Y; y++)
      {
        cells.Add(new Vector2I(OccupiedMainCell.X, y));
        cells.Add(new Vector2I(OccupiedMainCell.X + 1, y));
      }
    }

    return cells;
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    int score = 0;

    // Everything but the first 3 rows return score of 3,
    // This is to ensure tanky units are only placed in front.
    if (pos.Y <= GlobalConstants.GridSize.Y * 0.5f - 3)
      score += 3;

    // Negative score for each unit in front, because this unit damages every unit in front of it
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
