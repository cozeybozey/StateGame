using Godot;
using System.Collections.Generic;

public partial class GlobalSignals : Node
{
  [Signal]
  public delegate void GridEntityDiedEventHandler(GridEntity unit);

  [Signal]
  public delegate void GridEntitySpawnedEventHandler(GridEntity unit, bool playing);

  [Signal]
  public delegate void GridEntityMovedEventHandler(GridEntity unit, Vector2I oldCell, bool playing);

  [Signal]
  public delegate void SpeedChangedEventHandler(GridEntity unit);

  [Signal]
  public delegate void SizeChangedEventHandler(GridEntity unit, Godot.Collections.Array<Vector2I> oldOccupiedCells);

  [Signal]
  public delegate void DamageDealtEventHandler(GridEntity unit, int amount);

  [Signal]
  public delegate void HealingDoneEventHandler(GridEntity unit, int amount);

  [Signal]
  public delegate void DamageTakenEventHandler(GridEntity unit, int amount);

  [Signal]
  public delegate void HealingReceivedEventHandler(GridEntity unit, int amount);

  [Signal]
  public delegate void SideChangedEventHandler(Unit unit);

  [Signal]
  public delegate void UnitRemovedEventHandler(Unit unit);

  [Signal]
  public delegate void UnitInfoSelectedEventHandler(UnitInfo unitInfo);

  [Signal]
  public delegate void ExtraTurnGivenEventHandler(Unit originUnit, Unit targetUnit);
}