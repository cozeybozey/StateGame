using Godot;
using System;
using System.Collections.Generic;

public partial class Turret : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid)
  {
    foreach (Vector2I target in targets)
    {
      for (int i = 0; i < 3; i++)
      {
        Unit targetUnit = unitsGrid[target.X, target.Y];
        if (targetUnit != null)
        {
          targetUnit.ChangeHealth(-Damage, this);
        }

        Prop targetProp = propsGrid[target.X, target.Y];
        if (targetProp != null)
        {
          targetProp.ChangeHealth(-Damage, this);
        }
      }
    }
  }
}
