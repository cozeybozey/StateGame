using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

public partial class Shaman : Unit
{
  private Node _propsNode = null!;
  private List<PropInfo> _totems = new List<PropInfo>();

  protected override void Start()
  {
    _propsNode = GetTree().CurrentScene.GetNode("Props");
    foreach (PropInfo propInfo in GlobalConstants.PropsData.Values)
    {
      if (propInfo.Types.Contains("totem"))
        _totems.Add(propInfo);
    }
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Totems are 1 by 1
    Vector2I? randomPos = GlobalFunctions.GetRandomGridEntitySpawnLocation(unitsGrid, terrainGrid, propsGrid, [new Vector2I(0, 0)], Side);
    if (randomPos.HasValue)
      return [randomPos.Value];
    else
      return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Prop currentProp = propsGrid[target.X, target.Y];
      if (currentProp != null)
        currentProp.Die();

      PropInfo totemInfo = _totems[_rng.RandiRange(0, _totems.Count - 1)];

      // Spawn totem
      if (!GlobalFunctions.CanSpawnProp(totemInfo, target, unitsGrid, terrainGrid, propsGrid))
        continue;

      Prop propInstance = GD.Load<PackedScene>(totemInfo.ScenePath).Instantiate() as Prop;
      propInstance!.Initialize(totemInfo, target);
      _propsNode.AddChild(propInstance);
      propInstance.SpawnFloatingText("Created", Colors.Green);
    }
  }
}
