using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TotemOfFire : Prop
{
  public override void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Unit? randomFriendlyUnit = units
      .Where(u => u.Side == Side)
      .OrderBy(_ => GD.Randi())
      .FirstOrDefault();

    if (randomFriendlyUnit != null)
      randomFriendlyUnit.ChangeDamage(1);
  }
}