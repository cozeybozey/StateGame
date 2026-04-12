using Godot;
using System;
using System.Collections.Generic;

public partial class Betrayer : Unit
{
  Unit? _targetUnit = null;

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Make sure unit cannot target itself
    List<Unit> availableUnits = new List<Unit>();
    foreach (Unit unit in units)
    {
      if (unit != this)
        availableUnits.Add(unit);
    }

    if (availableUnits.Count == 0)
      return new List<Vector2I>();

    _targetUnit = availableUnits[_rng.RandiRange(0, availableUnits.Count - 1)];

    return [_targetUnit.OccupiedMainCell];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (_targetUnit != null)
    {
      _targetUnit.SwitchSides();
    }
  }
}
