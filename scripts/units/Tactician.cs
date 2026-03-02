using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Tactician : Unit
{
  public override int maxHealth { get; set; } = 10;
  public override int health { get; set; } = 10;
  public override int damage { get; set; } = 1;
  public override int armor { get; set; } = 0;
  public override int startingCooldown { get; set; } = 1;
  public override int cooldown { get; set; } = 1;
  public override int speed { get; set; } = 5;

  private GlobalSignals? _globalSignals;

  protected override void Initialize()
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
