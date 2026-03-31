using Godot;
using System;
using System.Collections.Generic;

public partial class Supporter : Unit
{
  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Predicate makes sure closest unit has to be friendly
    Tuple<Unit, Vector2I>? closest = GetClosestUnit(units, u => u.Side == Side);

    if (closest == null)
      return new List<Vector2I>();
    else
      return [closest.Item2];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeMaxHealth(Damage);
      }
    }
  }
}
