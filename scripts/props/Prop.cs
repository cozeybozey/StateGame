using Godot;
using System;
using System.Collections.Generic;

public partial class Prop : GridEntity
{
  public PropInfo StartPropInfo = null!;

  public void Initialize(PropInfo propInfo, Vector2I startCell, bool placed = false)
	{
    Id = propInfo.Id;
    DisplayName = propInfo.Name;
    ScenePath = propInfo.ScenePath;
    OccupiedCells = propInfo.OccupiedCells;
    Texture = propInfo.Texture;
    MaxHealth = propInfo.MaxHealth;
    Health = propInfo.Health;
    Damage = propInfo.Damage;
    Armor = propInfo.Armor;
    Description = propInfo.Description;
    Rarity = propInfo.Rarity;
    Damagable = propInfo.Damagable;
    Movable = propInfo.Movable;
    Blocking = propInfo.Blocking;
    Types = propInfo.Types;

    StartCell = startCell;
    Side = startCell.Y >= Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);
    StartPropInfo = propInfo;
    _placed = placed;
  }

	public PropInfo GetInfo()
	{
		return new PropInfo(Id, DisplayName, Texture, ScenePath, OccupiedCells, MaxHealth, Health, 
      Damage, Armor, Description, Rarity, Damagable, Movable, Blocking, Types);
	}

  public PropInfo GetStartInfo()
  {
		return StartPropInfo;
  }
}
