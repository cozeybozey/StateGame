using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Laser : Unit
{
  public override int maxHealth { get; set; } = 10;
  public override int health { get; set; } = 10;
  public override int damage { get; set; } = 10;
  public override int armor { get; set; } = 0;
  public override int startingCooldown { get; set; } = 1;
  public override int cooldown { get; set; } = 1;
  public override int speed { get; set; } = 10;

  private int _nrOfTargets = 16;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid)
  {
    Random random = new();

    List<Vector2I> cells = new();

    if (side)
    {
      for (int y = occupiedMainCell.Y - 1; y > 0; y--)
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
