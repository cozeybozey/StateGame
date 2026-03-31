using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DireWolf : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> positions = units
    .Where(u => u.Side == Side && u.Id == "wolf")
    .Select(u => u.OccupiedMainCell)
    .ToList();

    return positions;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit unit = unitsGrid[target.X, target.Y];
      if (unit == null)
        continue;
      foreach (Vector2I cell in GetSurroundingCells())
      {
        if (GlobalFunctions.CanMoveToCell(unit, cell, unitsGrid, terrainGrid, propsGrid))
          unit.MoveToCell(cell, true);
      }
    }
  }
}
