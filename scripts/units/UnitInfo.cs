using Godot;

public partial class UnitInfo : RefCounted
{
	public int Id { get; set; }
	public string Name { get; set; }
	public Texture2D Texture { get; set; }
  public string ScenePath { get; set; }
  public Unit? UnitInstance { get; set; }

	public UnitInfo(
		int id,
		string name,
		Texture2D texture,
		string scenePath,
		Unit? unitInstance)
	{
		Id = id;
		Name = name;
		Texture = texture;
		ScenePath = scenePath;
		UnitInstance = unitInstance;
	}
}
