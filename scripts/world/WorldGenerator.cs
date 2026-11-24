using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Creates the world: five sections of levels that branch out from the start in rings (layers), each ending in a boss
public class WorldGenerator
{
  public const int NumLayers = 10;
  public const int NumSections = 5;
  public static readonly string[] SectionNames = ["Earth", "Heaven", "Future", "Graveyard", "Hell"];

  private const int CoinsPerWin = 20;
  private const int GauntletLevels = 4;

  // Name and levels.json id of each section's boss
  private static readonly (string Name, string LevelId)[] Bosses =
    [("Earth Mother", "earth_mother"), ("Michael", "michael"), ("Nuke", "nuke"), ("Blob", "blob"), ("Lucifer", "lucifer")];

  private readonly Random _rng;
  private readonly LevelGenerator _levelGenerator;

  public WorldGenerator(Random rng)
  {
    _rng = rng;
    _levelGenerator = new LevelGenerator(rng);
  }

  // Fills `levels` by id, and `nodesPerLayer` with the number of levels per [layer, section]
  public void Generate(Godot.Collections.Dictionary<string, LevelInfo> levels, int[,] nodesPerLayer)
  {
    // Layer 0: single start node
    string startId = "Start";
    levels[startId] = CreateLevelInfo(startId, 0, 0, false, 0);
    for (int section = 0; section < NumSections; section++)
      nodesPerLayer[0, section] = 1;

    for (int q = 0; q < NumSections; q++)
    {
      List<List<LevelInfo>> sectionLayers = new();

      // Layer 1: one entry node per section
      string entryId = $"Q{q}_L1_N0";
      levels[entryId] = CreateLevelInfo(entryId, 1, 0, false, q);
      levels[startId].NextNodes.Add(entryId);
      sectionLayers.Add(new List<LevelInfo> { levels[entryId] });
      nodesPerLayer[1, q] = 1;

      // Remaining layers expand like a cone
      for (int layer = 2; layer < NumLayers; layer++)
      {
        bool isBoss = layer % (NumLayers - 1) == 0;
        int nodesInLayer = 0;
        if (layer == 2)
          nodesInLayer = nodesPerLayer[layer - 1, q] + 2;
        else if (layer < (NumLayers * 0.75f - 1))
          nodesInLayer = nodesPerLayer[layer - 1, q] + 1;
        else if (layer < (NumLayers - 1)) // After a while the layers converge to one boss layer
          nodesInLayer = nodesPerLayer[layer - 1, q] - 2;
        else
          nodesInLayer = 1;

        List<LevelInfo> layerNodes = new();

        for (int node = 0; node < nodesInLayer; node++)
        {
          string nodeId = isBoss
              ? $"Q{q}_L{layer}_Boss"
              : $"Q{q}_L{layer}_N{node}";
          levels[nodeId] = CreateLevelInfo(nodeId, layer, node, isBoss, q);
          layerNodes.Add(levels[nodeId]);
        }

        ConnectRings(sectionLayers[^1], layerNodes, isBoss);
        sectionLayers.Add(layerNodes);
        nodesPerLayer[layer, q] = nodesInLayer;
      }
    }
  }

  private void ConnectRings(List<LevelInfo> inner, List<LevelInfo> outer, bool isBoss)
  {
    if (isBoss)
    {
      foreach (LevelInfo node in inner)
        node.NextNodes.Add(outer[0].Id);
      return;
    }

    int innerCount = inner.Count;
    int outerCount = outer.Count;

    // Pass 1: map each inner node to its proportional outer node
    for (int i = 0; i < innerCount; i++)
    {
      int mapped = Mathf.Clamp(
          Mathf.RoundToInt((float)i / (innerCount == 1 ? 1 : innerCount - 1) * (outerCount - 1)),
          0, outerCount - 1);
      if (!inner[i].NextNodes.Contains(outer[mapped].Id))
        inner[i].NextNodes.Add(outer[mapped].Id);
    }

    // Ensure every outer node has at least one incoming connection
    for (int j = 0; j < outerCount; j++)
    {
      if (!inner.Any(n => n.NextNodes.Contains(outer[j].Id)))
      {
        int closest = Mathf.Clamp(
            Mathf.RoundToInt((float)j / (outerCount == 1 ? 1 : outerCount - 1) * (innerCount - 1)),
            0, innerCount - 1);
        inner[closest].NextNodes.Add(outer[j].Id);
      }
    }

    // Pass 2: add extra adjacent connections, using actual connected ranges
    // to prevent crossings
    for (int i = 0; i < innerCount; i++)
    {
      if (_rng.Next(0, 3) == 0) continue; // skip extra connections sometimes

      List<int> connected = Enumerable.Range(0, outerCount)
          .Where(j => inner[i].NextNodes.Contains(outer[j].Id))
          .ToList();

      int leftmost = connected.Min();
      int rightmost = connected.Max();

      // The strict bounds: never go past what neighboring inner nodes connect to
      int strictLeft = i > 0
          ? Enumerable.Range(0, outerCount)
              .Where(j => inner[i - 1].NextNodes.Contains(outer[j].Id))
              .DefaultIfEmpty(0).Max() // left neighbor's rightmost
          : 0;

      int strictRight = i < innerCount - 1
          ? Enumerable.Range(0, outerCount)
              .Where(j => inner[i + 1].NextNodes.Contains(outer[j].Id))
              .DefaultIfEmpty(outerCount - 1).Min() // right neighbor's leftmost
          : outerCount - 1;

      // Try extending left
      if (leftmost - 1 >= 0 && leftmost - 1 >= strictLeft && _rng.NextDouble() < 0.5)
        inner[i].NextNodes.Add(outer[leftmost - 1].Id);

      // Try extending right
      if (rightmost + 1 <= outerCount - 1 && rightmost + 1 <= strictRight && _rng.NextDouble() < 0.5)
        inner[i].NextNodes.Add(outer[rightmost + 1].Id);
    }
  }

  private LevelInfo CreateLevelInfo(string nodeId, int layer, int node, bool isBoss, int section)
  {
    if (isBoss)
    {
      (string bossName, string levelId) = Bosses[section];
      LevelBoard board = _levelGenerator.LoadLevel(levelId, section);
      return MakeLevel(bossName, boss: true, gauntlet: false, rewards: [], CoinsPerWin, [board]);
    }

    if (layer > 4 && _rng.NextDouble() < 0.2)
    {
      List<LevelBoard> boards = new();
      for (int i = 0; i < GauntletLevels; i++)
        boards.Add(_levelGenerator.CreateRandomLevel(layer, section));

      List<UnitInfo> rewardUnits = boards
          .SelectMany(board => board.Units.Cast<UnitInfo>())
          .Where(u => u != null)
          .DistinctBy(u => u.Id)
          .OrderBy(_ => _rng.Next())
          .Take(3)
          .ToList();

      return MakeLevel($"{SectionNames[section]}\nLayer {layer + 1}\nGauntlet {node + 1}", boss: false, gauntlet: true, rewardUnits, CoinsPerWin * 5, boards);
    }

    string name = layer == 0 ? "Start" : $"{SectionNames[section]}\nLayer {layer + 1}\nLevel {node + 1}";
    return MakeLevel(name, boss: false, gauntlet: false, rewards: [], CoinsPerWin, [_levelGenerator.CreateRandomLevel(layer + 1, section)]);

    LevelInfo MakeLevel(string levelName, bool boss, bool gauntlet, List<UnitInfo> rewards, int coinsReward, List<LevelBoard> boards)
    {
      return new LevelInfo(
          id: nodeId,
          name: levelName,
          completed: false,
          unlocked: layer == 0,
          layer: layer,
          layerIndex: node,
          boss: boss,
          gauntlet: gauntlet,
          rewards: rewards,
          coinsReward: coinsReward,
          levelSection: section,
          nextNodes: new List<string>(),
          terrains: boards.Select(b => b.Terrain).ToList(),
          props: boards.Select(b => b.Props).ToList(),
          units: boards.Select(b => b.Units).ToList()
      );
    }
  }
}
