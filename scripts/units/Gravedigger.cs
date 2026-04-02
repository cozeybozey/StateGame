using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Gravedigger : Unit
{
  protected override void Start()
  {
    _globalSignals.GridEntityDied += OnUnitDied;
  }

  private void OnUnitDied(GridEntity gridEntity)
  {
    if (!IsInsideTree() || gridEntity is not Unit unit)
      return;

    if (unit.Side == Side)
      ChangeDamage(1);
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
      _globalSignals.GridEntityDied -= OnUnitDied;
  }
}
