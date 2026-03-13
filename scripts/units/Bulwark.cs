using Godot;
using System;
using System.Collections.Generic;

public partial class Bulwark : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> deadUnits)
  {
    return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    ChangeArmor(1);
  }
}
