using Godot;
using System.Collections.Generic;

public partial class PropInfo : RefCounted
{
	public string Id { get; set; }
	public string Name { get; set; }
	public Texture2D Texture { get; set; }
  public string ScenePath { get; set; }
	public List<Vector2I> OccupiedCells { get; set; }
  public int MaxHealth { get; set; }
  public int Health { get; set; }
  public int Damage { get; set; }
  public int Armor { get; set; }
  public string Description { get; set; }
	public string Rarity { get; set; }
	public bool Damagable { get; set; }
	public bool Movable { get; set; }
	public bool Blocking { get; set; }
  public List<string> Types { get; set; }

  public PropInfo(
		string id,
		string name,
		Texture2D texture,
		string scenePath,
		List<Vector2I> occupiedCells,
    int maxHealth,
    int health,
    int damage,
    int armor,
    string description,
		string rarity,
		bool damagable,
		bool movable,
		bool blocking,
    List<string> types
  )
	{
		Id = id;
		Name = name;
		Texture = texture;
		ScenePath = scenePath;
		OccupiedCells = occupiedCells;
    MaxHealth = maxHealth;
    Health = health;
    Damage = damage;
    Armor = armor;
    Description = description;
    Rarity = rarity;
		Damagable = damagable;
		Movable = movable;
		Blocking = blocking;
    Types = types;
	}
}
