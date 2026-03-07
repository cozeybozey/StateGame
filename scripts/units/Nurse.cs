using Godot;
using System;
using System.Collections.Generic;

public partial class Nurse : Unit
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

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid)
  {
    Vector2I targetVector = new Vector2I(occupiedMainCell.X, occupiedMainCell.Y - 1);

    if (!side)
      targetVector = new Vector2I(occupiedMainCell.X, occupiedMainCell.Y + 1);

    if (targetVector.Y >= 0 && targetVector.Y < GlobalConstants.GridSize.Y && 
      unitsGrid[targetVector.X, targetVector.Y] != null && 
      unitsGrid[targetVector.X, targetVector.Y].side == side &&
      unitsGrid[targetVector.X, targetVector.Y].health < unitsGrid[targetVector.X, targetVector.Y].maxHealth)
        return [targetVector];
    else
      return [];
  }
}
