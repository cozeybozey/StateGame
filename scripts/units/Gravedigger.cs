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

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer back rows
    return (Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f) - pos.Y) + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
