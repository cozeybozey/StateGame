using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class FireElemental : Unit
{
  private Node _propsNode = null!;
  private PropInfo _fireInfo = null!;

  protected override void Start()
  {
    _propsNode = GetTree().CurrentScene.GetNode("Props");
    _fireInfo = GlobalConstants.PropsData["fire"];
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> candidates = new();
    int midY = Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f);

    int ystart = Side ? 0 : midY; // if we're player-side, enemies are top half (y < midY)
    int yend = Side ? midY : GlobalConstants.GridSize.Y; // exclusive

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = ystart; y < yend; y++)
      {
        Vector2I cell = new Vector2I(x, y);
        if (GlobalFunctions.CanSpawnProp(_fireInfo, cell, unitsGrid, terrainGrid, propsGrid))
          candidates.Add(cell);
      }
    }

    if (candidates.Count == 0)
      return [];

    // Pick up to 4 random distinct cells using Fisher-Yates partial shuffle
    int take = Math.Min(4, candidates.Count);
    List<Vector2I> targets = new();
    for (int i = 0; i < take; i++)
    {
      int j = _rng.RandiRange(i, candidates.Count - 1);
      // swap
      Vector2I tmp = candidates[i];
      candidates[i] = candidates[j];
      candidates[j] = tmp;
      targets.Add(candidates[i]);
    }

    return targets;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Unit targetUnit = unitsGrid[target.X, target.Y];
      if (targetUnit != null)
      {
        targetUnit.ChangeHealth(-Damage, this);
      }

      Prop targetProp = propsGrid[target.X, target.Y];
      if (targetProp != null)
      {
        targetProp.ChangeHealth(-Damage, this);
      }

      // Spawn fire
      if (!GlobalFunctions.CanSpawnProp(_fireInfo, target, unitsGrid, terrainGrid, propsGrid))
        continue;

      Prop propInstance = GD.Load<PackedScene>(_fireInfo.ScenePath).Instantiate() as Prop;
      propInstance!.Initialize(_fireInfo, target);
      _propsNode.AddChild(propInstance);
      propInstance.SpawnFloatingText("Created", Colors.Green);
    }
  }
}
