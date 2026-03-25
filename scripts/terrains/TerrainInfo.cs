using Godot;
using System.Collections.Generic;

public partial class TerrainInfo : RefCounted
{
	public string Id { get; set; }
	public string Name { get; set; }
	public Texture2D Texture { get; set; }
  public string ScenePath { get; set; }
	public List<Vector2I> OccupiedCells { get; set; }
	public string Description { get; set; }
	public string Rarity { get; set; }
	public bool Blocking { get; set; }
	public List<string> Types { get; set; }

	public TerrainInfo(
		string id,
		string name,
		Texture2D texture,
		string scenePath,
		List<Vector2I> occupiedCells,
		string description,
		string rarity,
		bool blocking,
		List<string> types
	)
	{
		Id = id;
		Name = name;
		Texture = texture;
		ScenePath = scenePath;
		OccupiedCells = occupiedCells;
		Description = description;
    Rarity = rarity;
		Blocking = blocking;
		Types = types;
	}
}
