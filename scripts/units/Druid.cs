using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Druid : Unit
{
  private GlobalSignals? _globalSignals;

  protected override void Start()
  {
    _globalSignals = GetNode<GlobalSignals>("/root/GlobalSignals");
    _globalSignals.GridEntitySpawned += OnGridEntitySpawned;
  }

  private void OnGridEntitySpawned(GridEntity gridEntity, bool playing)
  {
    if (!IsInsideTree() || gridEntity is not Prop prop || !playing)
      return;

    if (prop.Types.Contains("nature"))
      ChangeDamage(1);
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
      _globalSignals.GridEntitySpawned -= OnGridEntitySpawned;
  }
}
