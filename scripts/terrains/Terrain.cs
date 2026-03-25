using Godot;
using System;
using System.Collections.Generic;

public partial class Terrain : GridEntity
{
  public TerrainInfo StartTerrainInfo = null!;

  public void Initialize(TerrainInfo terrainInfo, Vector2I startCell, bool placed = false)
	{
    Damagable = false;
    Movable = false;

    Id = terrainInfo.Id;
    DisplayName = terrainInfo.Name;
    ScenePath = terrainInfo.ScenePath;
    OccupiedCells = terrainInfo.OccupiedCells;
    Texture = terrainInfo.Texture;
    Description = terrainInfo.Description;
    Rarity = terrainInfo.Rarity;
    Blocking = terrainInfo.Blocking;
    Types = terrainInfo.Types;

    StartCell = startCell;
    StartTerrainInfo = terrainInfo;
    _placed = placed;
  }

	public TerrainInfo GetInfo()
	{
		return new TerrainInfo(Id, DisplayName, Texture, ScenePath, OccupiedCells, Description, Rarity, Blocking, Types);
	}

  public TerrainInfo GetStartInfo()
  {
		return StartTerrainInfo;
  }
}
