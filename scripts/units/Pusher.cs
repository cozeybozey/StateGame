using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Pusher : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();
    for (int x = 0; x < OccupiedMainCell.X; x++)
    {
      Vector2I pos = new(x, OccupiedMainCell.Y);
      result.Add(pos);
    }
    return result;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Unit> movedUnits = new List<Unit>();

    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null && !movedUnits.Contains(targetUnit))
      {
        Vector2I targetCell = targetUnit.OccupiedMainCell + new Vector2I(-1, 0);
        if (GlobalFunctions.CanMoveToCell(targetUnit, targetCell, unitsGrid, terrainGrid, propsGrid))
        {
          targetUnit.MoveToCell(targetCell, playing: true);
          movedUnits.Add(targetUnit);
        }
      }
    }
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Increase score for each extra unit that can be pushed
    int score = 0;

    for (int x = 0; x < pos.X; x++)
    {
      if (unitsGrid[x, pos.Y] != null)
        score += 5;
    }    

    return score + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
