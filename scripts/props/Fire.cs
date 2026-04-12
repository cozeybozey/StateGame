using Godot;
using System;
using System.Collections.Generic;

public partial class Fire : Prop
{
  public override void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Unit unit = unitsGrid[OccupiedMainCell.X, OccupiedMainCell.Y];
    if (unit != null)
    {
      unit.ChangeHealth(-Damage, this);
    }
  }
}
