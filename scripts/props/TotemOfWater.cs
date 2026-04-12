using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TotemOfWater : Prop
{
  public override void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Unit? randomFriendlyUnit = units
      .Where(u => u.Side == Side && u.Health < u.MaxHealth)
      .OrderBy(_ => GD.Randi())
      .FirstOrDefault();

    if (randomFriendlyUnit != null)
      randomFriendlyUnit.ChangeHealth(2, this);
  }
}