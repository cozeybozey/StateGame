using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Templar : Unit
{
  public override void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    ChangeDamage(1);
  }
}
