using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

public partial class Cherub : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> targets = new();
    int frontDir = Side ? -1 : 1;

    // Find frontmost unit in same column
    foreach (Vector2I cell in GetOccupiedCells())
    {
      int checkY = cell.Y + frontDir;
      while (GlobalFunctions.IsCellInsideGrid(new Vector2I(cell.X, checkY)))
      {
        Unit unit = unitsGrid[cell.X, checkY];
        if (unit != null)
        {
          targets.Add(unit.OccupiedMainCell);
          return targets;
        }
        checkY += frontDir;
      }
    }

    return targets;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets.Count == 0) return;

    Unit target = unitsGrid[targets[0].X, targets[0].Y];
    if (target == null) return;

    target.ChangeDamage(Damage);
    target.ChangeSpeed(Damage);
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Prefer back rows
    int score = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f) - pos.Y;

    // Extra score for each unit in front
    foreach (Vector2I cell in unitInfo.OccupiedCells)
    {
      Vector2I newPos = pos + cell;
      int checkY = newPos.Y + 1;
      while (GlobalFunctions.IsCellInsideGrid(new Vector2I(newPos.X, checkY)))
      {
        UnitInfo unit = unitsGrid[newPos.X, checkY];
        if (unit != null)
        {
          score += 5;
        }
        checkY += 1;
      }
    }

    return score + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
