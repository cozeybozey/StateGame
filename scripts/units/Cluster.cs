using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Cluster : Unit
{
  public override int maxHealth { get; set; } = 15;
  public override int health { get; set; } = 15;
  public override int damage { get; set; } = 8;
  public override int armor { get; set; } = 0;
  public override int startingCooldown { get; set; } = 1;
  public override int cooldown { get; set; } = 1;
  public override int speed { get; set; } = 3;

  private int _nrOfTargets = 16;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid)
  {
    Random random = new();

    List<Vector2I> allCells = new();

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      if (side)
      {
        for (int y = 0; y < GlobalConstants.GridSize.Y / 2; y++)
        {
          allCells.Add(new Vector2I(x, y));
        }
      }
      else
      {
        for (int y = GlobalConstants.GridSize.Y / 2; y < GlobalConstants.GridSize.Y; y++)
        {
          allCells.Add(new Vector2I(x, y));
        }
      }
    }

    return allCells
        .OrderBy(_ => random.Next())
        .Take(_nrOfTargets)
        .ToList();
  }
}
