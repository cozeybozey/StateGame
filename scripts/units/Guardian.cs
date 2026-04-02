using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class Guardian : Unit
{
  private List<Unit> _unitsToAct = new();
  private List<Unit> _buffedUnits = new();

  protected override void Start()
  {
    _globalSignals.GridEntitySpawned += OnUnitSpawned;
    _globalSignals.GridEntityMoved += OnUnitMoved;
    _globalSignals.SizeChanged += OnUnitSizeChanged;
    _globalSignals.SideChanged += OnUnitSideChanged;
  }

  public override bool CanAct()
  {
    return false;
  }


  public override void GameStart(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units)
  {
    ApplyBuff(units, unitsGrid);
    _unitsToAct = units;
  }

  private void OnUnitSpawned(GridEntity gridEntity, bool playing)
  {
    if (!playing || gridEntity is not Unit unit) return;
    if (IsUnitBehind(unit))
      AddBuff(unit);
  }

  private void OnUnitMoved(GridEntity gridEntity, Vector2I oldCell, bool playing)
  {
    if (!playing || gridEntity is not Unit unit) return;
    RefreshBuffs();
  }

  private void OnUnitSizeChanged(GridEntity gridEntity, Godot.Collections.Array<Vector2I> oldOccupiedCells)
  {
    RefreshBuffs();
  }

  private void OnUnitSideChanged(Unit unit)
  {
    RefreshBuffs();
  }

  private bool IsUnitBehind(Unit unit)
  {
    if (unit == this) return false;

    int behindDir = Side ? 1 : -1;

    return GetOccupiedCells().Any(myCell =>
        unit.GetOccupiedCells().Any(theirCell =>
            theirCell.X == myCell.X &&
            (Side ? theirCell.Y > myCell.Y : theirCell.Y < myCell.Y)));
  }

  private void AddBuff(Unit unit)
  {
    unit.Armor += 2;
    _buffedUnits.Add(unit);
  }

  private void RemoveBuff(Unit unit)
  {
    unit.Armor -= 2;
    _buffedUnits.Remove(unit);
  }

  private void ApplyBuff(List<Unit> units, Unit[,] unitsGrid)
  {
    foreach (Unit unit in units)
    {
      if (IsUnitBehind(unit) && !_buffedUnits.Contains(unit))
        AddBuff(unit);
    }
  }

  private void RefreshBuffs()
  {
    // Remove buffs from units no longer behind
    foreach (Unit unit in _buffedUnits)
    {
      if (!IsUnitBehind(unit))
        RemoveBuff(unit);
    }

    // Add buffs to newly eligible units
    foreach (Unit unit in _unitsToAct)
    {
      if (IsUnitBehind(unit) && !_buffedUnits.Contains(unit))
        AddBuff(unit);
    }
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
    {
      _globalSignals.GridEntitySpawned -= OnUnitSpawned;
      _globalSignals.GridEntityMoved -= OnUnitMoved;
      _globalSignals.SizeChanged -= OnUnitSizeChanged;
    }
  }
}
