using Godot;
using System;
using System.Collections.Generic;
using static System.Formats.Asn1.AsnWriter;

public partial class Tank : Unit
{
  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer front rows
    return pos.Y + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
