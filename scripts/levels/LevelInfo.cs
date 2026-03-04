using Godot;
using System.Collections.Generic;

public partial class LevelInfo : RefCounted
{
	public string Id { get; set; }
	public string Name { get; set; }
	public bool Completed { get; set; }
	public bool Unlocked { get; set; }
  public int Layer { get; set; }
	public int LayerIndex { get; set; }
  public bool IsBoss { get; set; }
  public List<string> NextNodes { get; set; }
	public Button LevelButton { get; set; }
	public UnitInfo[,]? Units { get; set; }

	public LevelInfo(
		string id,
		string name,
		bool completed,
		bool unlocked,
		int layer,
		int layerIndex,
		bool isBoss,
		List<string> nextNodes,
		Button levelButton,
		UnitInfo[,]? units)
	{
		Id = id;
		Name = name;
    Completed = completed;
		Unlocked = unlocked;
		Layer = layer;
		LayerIndex = layerIndex;
		IsBoss = isBoss;
		NextNodes = nextNodes;
		LevelButton = levelButton;
    Units = units;
	}
}
