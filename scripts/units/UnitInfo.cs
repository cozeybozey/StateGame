using Godot;
using System.Collections.Generic;

public partial class UnitInfo : RefCounted
{
	public string Id { get; set; }
	public string Name { get; set; }
	public Texture2D Texture { get; set; }
  public string ScenePath { get; set; }
	public List<Vector2I> OccupiedCells { get; set; }
	public int Cost { get; set; }
	public int Health { get; set; }
	public int Damage { get; set; }
	public int Armor { get; set; }
	public int Speed { get; set; }
	public int Cooldown { get; set; }
	public string Description { get; set; }
  public Unit? UnitInstance { get; set; }

	public UnitInfo(
		string id,
		string name,
		Texture2D texture,
		string scenePath,
		List<Vector2I> occupiedCells,
		int cost,
		int health,
		int damage,
		int armor,
		int speed,
		int cooldown,
		string description,
		Unit? unitInstance)
	{
		Id = id;
		Name = name;
		Texture = texture;
		ScenePath = scenePath;
		OccupiedCells = occupiedCells;
		Cost = cost;
		Health = health;
		Damage = damage;
		Armor = armor;
		Speed = speed;
		Cooldown = cooldown;
		Description = description;
    UnitInstance = unitInstance;
	}
}
