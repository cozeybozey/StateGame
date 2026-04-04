using Godot;
using System;
using System.Collections.Generic;

public partial class Berserker : Unit
{
  public override int ChangeHealth(int amount, GridEntity? unit)
  {
    int extraDamage = MaxHealth - Health;
    int effectiveAmount = base.ChangeHealth(amount, unit);
    if (amount > 0)
      ChangeDamage(Mathf.Min(amount, extraDamage));
    return effectiveAmount;
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer front rows
    return pos.Y + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
