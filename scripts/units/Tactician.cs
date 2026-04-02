using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Tactician : Unit
{

  protected override void Start()
  {
    _globalSignals.GridEntityMoved += OnUnitMoved;
  }

  private void OnUnitMoved(GridEntity gridEntity, Vector2I oldCell, bool playing)
  {
    if (!playing)
      return;

    if (!IsInsideTree() || gridEntity is not Unit unit)
      return;

    if (unit.Side == Side)
      ChangeDamage(1);
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
      _globalSignals.GridEntityMoved -= OnUnitMoved;
  }
}
