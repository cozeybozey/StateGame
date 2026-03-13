using Godot;
using System;
using System.Collections.Generic;

public partial class Betrayer : Unit
{
  Unit? _targetUnit = null;

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    if (_targetUnit != null)
    {
      _targetUnit.side = !_targetUnit.side;
      _targetUnit.SwitchedSides = !SwitchedSides;
      _targetUnit.SpawnFloatingText("Switched sides", Colors.Red);
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    if (units == null || units.Count == 0)
      return new List<Vector2I>();

    _targetUnit = units[_rng.RandiRange(0, units.Count - 1)];

    return [_targetUnit.occupiedMainCell];
  }
}
