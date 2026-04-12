using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

public partial class Mover : Unit
{
  private bool _movingRight = true;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> result = new List<Vector2I>();

    int dir = _movingRight ? 1 : -1;
    bool moved = false;

    // Check primary direction
    if (GlobalFunctions.CanMoveToCell(this, new Vector2I(OccupiedMainCell.X + dir, OccupiedMainCell.Y), unitsGrid, terrainGrid, propsGrid))
    {
      moved = true;
      MoveToCell(OccupiedMainCell + new Vector2I(dir, 0), playing: true);
    }
    // Otherwise suggest opposite direction if possible (but do not change state)
    else if (GlobalFunctions.CanMoveToCell(this, new Vector2I(OccupiedMainCell.X - dir, OccupiedMainCell.Y), unitsGrid, terrainGrid, propsGrid))
    {
      moved = true;
      MoveToCell(OccupiedMainCell + new Vector2I(-dir, 0), playing: true);
    }

    if (!moved)
      return [];

    // Return surrounding cells
    for (int x = - 1; x <= 1; x++)
    {
      for (int y = -1; y <= 1; y++)
      {
        Vector2I cell = new Vector2I(x, y);
        if (cell != new Vector2I(0, 0) && GlobalFunctions.IsCellInsideGrid(OccupiedMainCell + cell))
          result.Add(OccupiedMainCell + cell);
      }
    }

    return result;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Buff surrounding units
    List<Unit> buffed = new List<Unit>();
    foreach (Vector2I cell in targets)
    {
      Unit unit = unitsGrid[cell.X, cell.Y];
      if (unit == null || unit == this)
        continue;
      if (buffed.Contains(unit))
        continue;
      buffed.Add(unit);
      unit.ChangeDamage(1);
      unit.ChangeMaxHealth(1);
    }
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    int score = 0;

    // Everything but the first 3 rows return score of 3,
    // This is to ensure tanky units are only placed in front.
    if (pos.Y <= GlobalConstants.GridSize.Y * 0.5f - 3)
      score += 3;

    // Increase score for every unit that the mover will buff when moving from left to right
    // Gain more score for units that are closer
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      foreach (int y in new[] { -1, 1 })
      {
        if (GlobalFunctions.IsCellInsideGrid(new Vector2I(pos.X, pos.Y + y)) && unitsGrid[x, pos.Y + y] != null)
          score += GlobalConstants.GridSize.X - Mathf.Abs(pos.X - x);
      }
    }

    return score + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
