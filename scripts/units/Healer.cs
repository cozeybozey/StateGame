using Godot;
using System;
using System.Collections.Generic;

public partial class Healer : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(Damage, this);
      }
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (Side)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          if (unitsGrid[x, y] != null && unitsGrid[x, y].Side == Side && unitsGrid[x, y].Health < unitsGrid[x, y].MaxHealth)
            return [new Vector2I(x, y)];
        }
      }
    }
    else
    {
      for (int y = GlobalConstants.GridSize.Y - 1; y >= 0; y--)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          if (unitsGrid[x, y] != null && unitsGrid[x, y].Side == Side && unitsGrid[x, y].Health < unitsGrid[x, y].MaxHealth)
            return [new Vector2I(x, y)];
        }
      }
    }

    return [];
  }
}
