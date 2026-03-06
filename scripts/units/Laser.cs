using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Laser : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid)
  {
    List<Vector2I> cells = new();

    if (side)
    {
      for (int y = occupiedMainCell.Y - 1; y >= 0; y--)
      {
        cells.Add(new Vector2I(occupiedMainCell.X, y));
        cells.Add(new Vector2I(occupiedMainCell.X + 1, y));
      }
    }
    else
    {
      for (int y = occupiedMainCell.Y + 1; y < GlobalConstants.GridSize.Y; y++)
      {
        cells.Add(new Vector2I(occupiedMainCell.X, y));
        cells.Add(new Vector2I(occupiedMainCell.X + 1, y));
      }
    }

    return cells;
  }
}
