using Godot;
using System;
using System.Collections.Generic;

public partial class Vampire : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(-damage, this);
        ChangeHealth(damage, this);
      }
    }
  }
}
