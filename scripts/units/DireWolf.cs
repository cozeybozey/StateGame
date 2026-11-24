using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DireWolf : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> positions = units
    .Where(u => u.Side == Side && u.Id == "wolf" && !IsNextTo(u))
    .Select(u => u.OccupiedMainCell)
    .ToList();

    return positions;
  }

  private bool IsNextTo(Unit unit)
  {
    return GetSurroundingCells(includeDiagonals: true).Contains(unit.OccupiedMainCell);
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit unit = unitsGrid[target.X, target.Y];
      // A wolf can have moved next to this unit by itself during the wind-up
      if (unit == null || IsNextTo(unit))
        continue;
      foreach (Vector2I cell in GetSurroundingCells(includeDiagonals: true))
      {
        if (GlobalFunctions.CanMoveToCell(unit, cell, unitsGrid, terrainGrid, propsGrid))
        {
          unit.MoveToCell(cell, true);
          break;
        }
      }
    }
  }
}
