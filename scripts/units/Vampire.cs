using Godot;
using System;
using System.Collections.Generic;

public partial class Vampire : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(-Damage, this);
        ChangeHealth(Damage, this);
      }
    }
  }
}
