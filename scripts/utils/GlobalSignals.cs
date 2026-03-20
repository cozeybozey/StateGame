using Godot;
using System.Collections.Generic;

public partial class GlobalSignals : Node
{
  [Signal]
  public delegate void UnitDiedEventHandler(Unit unit);

  [Signal]
  public delegate void UnitSpawnedEventHandler(Unit unit, bool playing);

  [Signal]
  public delegate void UnitMovedEventHandler(Unit unit, Vector2I oldCell, bool playing);

  [Signal]
  public delegate void SpeedChangedEventHandler(Unit unit);

  [Signal]
  public delegate void SizeChangedEventHandler(Unit unit, Godot.Collections.Array<Vector2I> oldOccupiedCells);

  [Signal]
  public delegate void SideChangedEventHandler(Unit unit);

  [Signal]
  public delegate void DamageDealtEventHandler(Unit unit, int amount);

  [Signal]
  public delegate void HealingDoneEventHandler(Unit unit, int amount);

  [Signal]
  public delegate void DamageTakenEventHandler(Unit unit, int amount);

  [Signal]
  public delegate void HealingReceivedEventHandler(Unit unit, int amount);

  [Signal]
  public delegate void UnitRemovedEventHandler(Unit unit);

  [Signal]
  public delegate void UnitInfoSelectedEventHandler(UnitInfo unitInfo);
}