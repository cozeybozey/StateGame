using Godot;
using System.Collections.Generic;

public partial class UnitInfo : RefCounted
{
	public int Id { get; set; }
	public string Name { get; set; }
	public Texture2D Texture { get; set; }
  public string ScenePath { get; set; }
	public List<Vector2I> OccupiedCells { get; set; }
	public int Cost { get; set; }
  public Unit? UnitInstance { get; set; }

	public UnitInfo(
		int id,
		string name,
		Texture2D texture,
		string scenePath,
		List<Vector2I> occupiedCells,
		int cost,
		Unit? unitInstance)
	{
		Id = id;
		Name = name;
		Texture = texture;
		ScenePath = scenePath;
		OccupiedCells = occupiedCells;
		Cost = cost;
    UnitInstance = unitInstance;
	}
}
