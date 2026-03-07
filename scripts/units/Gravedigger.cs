using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Gravedigger : Unit
{
  private GlobalSignals? _globalSignals;

  protected override void Start()
  {
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _globalSignals.UnitDied += OnUnitDied;
  }

  private void OnUnitDied(Unit unit)
  {
    if (!IsInsideTree())
      return;

    if (unit.side == side)
      ChangeDamage(2);
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
      _globalSignals.UnitDied -= OnUnitDied;
  }
}
