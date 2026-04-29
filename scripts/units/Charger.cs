using Godot;
using System;
using System.Collections.Generic;

public partial class Charger : Unit
{
  private int _damageTaken = 0;

  public override int ChangeHealth(int amount, GridEntity? unit)
  {
    int effectiveAmount = base.ChangeHealth(amount, unit);
    _damageTaken += effectiveAmount;
    if (_damageTaken >= 15)
    {
      _damageTaken = 0;
      Tuple<Unit, Vector2I>? farthestEnemy = GetFarthestUnit(_units, u => u.Side != Side);
      if (farthestEnemy != null)
        farthestEnemy.Item1.ChangeHealth(-Damage, this);
    }
    return effectiveAmount;
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer front rows
    return pos.Y + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
