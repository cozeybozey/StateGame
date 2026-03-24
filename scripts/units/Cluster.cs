using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Cluster : Unit
{
  private int _nrOfTargets = 16;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Random random = new();

    List<Vector2I> allCells = new();

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      if (Side)
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
