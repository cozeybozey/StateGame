using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class Templar : Unit
{
  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    base.Act(targets, unitsGrid, terrainGrid, propsGrid, units, deadUnits);
    ChangeDamage(1);
  }
}
