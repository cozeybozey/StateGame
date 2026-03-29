using Godot;
using System;
using System.Collections.Generic;

public partial class Ghoul : Unit
{
  public override void DeathRattle(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (Side)
    {
      for (int y = GlobalConstants.GridSize.Y - 1; y >= 0; y--)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          Unit unit = unitsGrid[x, y];
          if (unit != null && unit.Side != Side)
          {
            unit.ChangeHealth(-Damage, this);
            return;
          }
        }
      }
    }
    else
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        for (int x = 0; x < GlobalConstants.GridSize.X; x++)
        {
          Unit unit = unitsGrid[x, y];
          if (unit != null && unit.Side != Side)
          {
            unit.ChangeHealth(-Damage, this);
            return;
          }
        }
      }
    }
  }
}
