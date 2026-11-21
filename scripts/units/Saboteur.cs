using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Saboteur : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new();
    for (int dx = -2; dx <= 2; dx++)
    {
      for (int dy = -2; dy <= 2; dy++)
      {
        int distance = Mathf.Abs(dx) + Mathf.Abs(dy);

        if (distance == 0 || distance > 2)
          continue;

        Vector2I pos = new(OccupiedMainCell.X + dx, OccupiedMainCell.Y + dy);

        if (!GlobalFunctions.IsCellInsideGrid(pos))
          continue;

        result.Add(pos);
      }
    }

    if (Side)
    {
      if (!GlobalFunctions.CanMoveToCell(this, new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y - 1), unitsGrid, terrainGrid, propsGrid))
      {
        return result;
      }
    }
    else
    {
      if (!GlobalFunctions.CanMoveToCell(this, new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y + 1), unitsGrid, terrainGrid, propsGrid))
      {
        return result;
      }
    }
    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (targets.Count == 0)
    {
      // Walk forward. If the cell got taken during the wind-up, explode instead (what it does when blocked).
      Vector2I forward = new Vector2I(OccupiedMainCell.X, OccupiedMainCell.Y + (Side ? -1 : 1));
      if (TryMoveToCell(forward))
        return;
      targets = GetTargets(unitsGrid, terrainGrid, propsGrid, units, deadUnits);
      if (targets.Count == 0)
        return;
    }

    if (targets.Count > 0)
    {
      foreach (Vector2I target in targets)
      {
        Unit targetUnit = unitsGrid[target.X, target.Y];
        if (targetUnit != null)
        {
          targetUnit.ChangeHealth(-Damage, this);
        }
      }
      Die(); // Sacrifice self
    }
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer front rows
    return pos.Y + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
