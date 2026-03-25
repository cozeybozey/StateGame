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
}
