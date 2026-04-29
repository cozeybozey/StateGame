using Godot;
using System;
using System.Collections.Generic;

public partial class Zombie : Unit
{
  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer front rows
    return pos.Y + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
