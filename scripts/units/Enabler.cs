using Godot;
using System;
using System.Collections.Generic;

public partial class Enabler : Unit
{
    public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Predicate makes sure closest unit has to be friendly and not another enabler
    Tuple<Unit, Vector2I>? closest = GetClosestUnit(units, u => u.Side == Side && u is not Enabler);

    if (closest == null)
      return new List<Vector2I>();
    else
      return [closest.Item2];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    return; // Temporarily disable, because it does not work with our current system.

    //foreach (Vector2I target in targets)
    //{
    //  Unit unit = unitsGrid[target.X, target.Y];

    //  // Insert the unit after this unit so it immediately gets to play after this unit's turn
    //  if (unit != null)
    //    _globalSignals.EmitSignal(GlobalSignals.SignalName.ExtraTurnGiven, this, unit);
    //}
  }
}
