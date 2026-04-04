using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

public partial class Archangel : Unit
{

  protected override void Start()
  {
    _globalSignals.DamageTaken += OnUnitDamageTaken;
  }

  public override bool CanAct()
  {
    return false;
  }

  private void OnUnitDamageTaken(GridEntity gridEntity, int amount)
  {
    if (!IsInsideTree() || gridEntity is not Unit unit || unit.Id == "archangel")
      return;

    if (unit.Side != Side || unit.Health <= 0)
      return;

    unit.ChangeHealth(Mathf.FloorToInt(amount * 0.5f), this);

    // Damage self for 50% of the amount, ignoring armor
    int currentArmor = Armor;
    Armor = 0;
    ChangeHealth(Mathf.FloorToInt(amount * -0.5f), this);
    Armor = currentArmor;
  }

  public override void _ExitTree()
  {
    if (_globalSignals != null)
      _globalSignals.DamageTaken -= OnUnitDamageTaken;
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer back rows
    return (Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f) - pos.Y) + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
