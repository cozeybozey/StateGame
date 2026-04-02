using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;

public partial class PitFiend : Unit
{

  protected override void Start()
  {
    _globalSignals.GridEntityDied += OnUnitDied;
    _globalSignals.GridEntitySpawned += OnUnitSpawned;
  }

  public override void GameStart(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units)
  {
    int extraMaxHealth = 0;
    foreach (Unit unit in units)
    {
      if (unit.Side == Side && unit.Types.Contains("demonic") && unit != this)
        extraMaxHealth++;
    }

    if (extraMaxHealth > 0)
      ChangeMaxHealth(extraMaxHealth);
  }

  private void OnUnitDied(GridEntity gridEntity)
  {
    if (!IsInsideTree() || gridEntity is not Unit unit)
      return;

    if (unit.Side == Side && unit.Types.Contains("demonic"))
      ChangeMaxHealth(-1);
  }

  private void OnUnitSpawned(GridEntity gridEntity, bool playing)
  {
    if (!IsInsideTree() || gridEntity is not Unit unit || !playing)
      return;

    if (unit.Side == Side && unit.Types.Contains("demonic"))
      ChangeMaxHealth(1);
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
    {
      _globalSignals.GridEntityDied -= OnUnitDied;
      _globalSignals.GridEntitySpawned -= OnUnitSpawned;
    }
  }
}
