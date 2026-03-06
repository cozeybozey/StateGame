using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Tactician : Unit
{
  private GlobalSignals? _globalSignals;

  protected override void Start()
  {
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _globalSignals.UnitMoved += OnUnitMoved;
  }

  private void OnUnitMoved(Unit unit, Vector2I oldCell)
  {
    if (!IsInsideTree())
      return;

    if (unit.side == side)
      ChangeDamage(1);
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
      _globalSignals.UnitMoved -= OnUnitMoved;
  }
}
