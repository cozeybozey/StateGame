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
  public bool Boss { get; set; }
  public bool Gauntlet { get; set; }
	public List<UnitInfo> Rewards { get; set; }
	public int CoinsReward { get; set; }
  public int LevelSection { get; set; }
  public List<string> NextNodes { get; set; }
  public List<TerrainInfo[,]> Terrains { get; set; }
  public List<PropInfo[,]> Props { get; set; }
  public List<UnitInfo[,]> Units { get; set; }

	public LevelInfo(
		string id,
		string name,
		bool completed,
		bool unlocked,
		int layer,
		int layerIndex,
		bool boss,
		bool gauntlet,
		List<UnitInfo> rewards,
		int coinsReward,
		int levelSection,
		List<string> nextNodes,
    List<TerrainInfo[,]> terrains,
    List<PropInfo[,]> props,
    List<UnitInfo[,]> units)
	{
		Id = id;
		Name = name;
    Completed = completed;
		Unlocked = unlocked;
		Layer = layer;
		LayerIndex = layerIndex;
		Boss = boss;
		Gauntlet = gauntlet;
		Rewards = rewards;
		CoinsReward = coinsReward;
		LevelSection = levelSection;
		NextNodes = nextNodes;
    Terrains = terrains;
		Props = props;
    Units = units;
	}
}
