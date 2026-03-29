using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Succubus : Unit
{

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid)
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
}
