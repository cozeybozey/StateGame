using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Bomber : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    LineOfSight.Target? found = LineOfSight.FindRandomVisible(GetOccupiedCells(), Side, units, terrainGrid, propsGrid, _rng);
    if (found == null)
      return [];
    if (found.Obstacle)
      return SelectTarget(found);
    Vector2I target = found.Cell;

    List<Vector2I> cells = new();
    for (int dy = -1; dy <= 1; dy++)
      for (int dx = -1; dx <= 1; dx++)
      {
        Vector2I cell = new(target.X + dx, target.Y + dy);
        if (cell.X >= 0 && cell.X < GlobalConstants.GridSize.X &&
            cell.Y >= 0 && cell.Y < GlobalConstants.GridSize.Y)
          cells.Add(cell);
      }

    return cells;
  }

}
