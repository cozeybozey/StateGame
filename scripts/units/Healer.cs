using Godot;
using System;
using System.Collections.Generic;

public partial class Healer : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(damage);
      }
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> deadUnits)
  {
    if (side)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          if (unitsGrid[x, y] != null && unitsGrid[x, y].side == side && unitsGrid[x, y].health < unitsGrid[x, y].maxHealth)
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
          if (unitsGrid[x, y] != null && unitsGrid[x, y].side == side && unitsGrid[x, y].health < unitsGrid[x, y].maxHealth)
            return [new Vector2I(x, y)];
        }
      }
    }

    return [];
  }
}
