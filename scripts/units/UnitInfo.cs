using Godot;
using System;
using System.Collections.Generic;

public partial class UnitInfo : RefCounted
{
	public string Id { get; set; }
	public string Name { get; set; }
	public Texture2D Texture { get; set; }
  public string ScenePath { get; set; }
	public List<Vector2I> OccupiedCells { get; set; }
	public int Cost { get; set; }
	public int MaxHealth { get; set; }
	public int Health { get; set; }
	public int Damage { get; set; }
	public int Armor { get; set; }
	public int Speed { get; set; }
	public int StartCooldown { get; set; }
	public int Cooldown { get; set; }
	public string Description { get; set; }
	public string Rarity { get; set; }
	public List<string> Types { get; set; }
	public int Stage { get; set; }
  public Func<Vector2I, UnitInfo, UnitInfo[,], TerrainInfo[,], PropInfo[,], int> ScorePlacement { get; set; } = (pos, unitInfo, unitsGrid, terrainGrid, propsGrid) => 0;

  public UnitInfo(
		string id,
		string name,
		Texture2D texture,
		string scenePath,
		List<Vector2I> occupiedCells,
		int cost,
		int maxHealth,
		int health,
		int damage,
		int armor,
		int speed,
		int startCooldown,
		int cooldown,
		string description,
		string rarity,
		List<string> types,
		int stage)
	{

    Id = id;
		Name = name;
		Texture = texture;
		ScenePath = scenePath;
		OccupiedCells = occupiedCells;
		Cost = cost;
		MaxHealth = maxHealth;
		Health = health;
		Damage = damage;
		Armor = armor;
		Speed = speed;
		StartCooldown = startCooldown;
		Cooldown = cooldown;
		Description = description;
    Rarity = rarity;
		Types = types;
		Stage = stage;
  }
}
