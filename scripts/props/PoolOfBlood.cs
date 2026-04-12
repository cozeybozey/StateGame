using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PoolOfBlood : Prop
{
  public override void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Unit unit = unitsGrid[OccupiedMainCell.X, OccupiedMainCell.Y];

    if (unit != null && unit.Types.Contains("demonic"))
      unit.ChangeHealth(2, this);
  }
}