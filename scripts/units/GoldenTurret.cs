using Godot;
using System;
using System.Collections.Generic;

public partial class GoldenTurret : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Damage = 0;
    int friendlyMachineryUnitCount = 0;
    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        Unit unit = unitsGrid[x, y];
        if (unit != null && unit.Side == Side && unit.Types.Contains("machinery"))
          friendlyMachineryUnitCount++;
      }
    }
    Damage = Mathf.FloorToInt(friendlyMachineryUnitCount * 0.5f);

    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(-Damage, this);
      }
    }
  }
}
