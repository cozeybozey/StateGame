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
    _buffedUnits.Clear();

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

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer front rows
    int score = pos.Y;

    // Bonus score for being in front of other units
    List<UnitInfo> countedUnits = new List<UnitInfo>();
    foreach (Vector2I cell in unitInfo.OccupiedCells)
    {
      Vector2I newPos = pos + cell;
      int checkY = newPos.Y - 1;
      while (GlobalFunctions.IsCellInsideGrid(new Vector2I(newPos.X, checkY)))
      {
        if (unitsGrid[newPos.X, checkY] != null && !countedUnits.Contains(unitsGrid[newPos.X, checkY]))
        {
          score++;
          countedUnits.Add(unitsGrid[newPos.X, checkY]);
        }
        checkY--;
      }
    }

    return score + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
