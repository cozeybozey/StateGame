using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

public partial class Pyromancer : Unit
{
  private Node _propsNode = null!;
  private PropInfo _wildFireInfo = null!;

  protected override void Start()
  {
    _propsNode = GetTree().CurrentScene.GetNode("Props");
    _wildFireInfo = GlobalConstants.PropsData["wild_fire"];
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> fireCells = new List<Vector2I>();

    for (int x = 0; x < GlobalConstants.GridSize.X; x++)
    {
      for (int y = 0; y < GlobalConstants.GridSize.Y; y++)
      {
        Vector2I cell = new Vector2I(x, y);
        Prop prop = propsGrid[x, y];
        if (prop != null && prop.Id == "fire")
        {
          fireCells.Add(cell);
        }
      }
    }

    return fireCells;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      Prop currentProp = propsGrid[target.X, target.Y];
      if (currentProp != null)
        currentProp.Die();

      // Spawn wild fire
      if (!GlobalFunctions.CanSpawnProp(_wildFireInfo, target, unitsGrid, terrainGrid, propsGrid))
        continue;

      Prop propInstance = GD.Load<PackedScene>(_wildFireInfo.ScenePath).Instantiate() as Prop;
      propInstance!.Initialize(_wildFireInfo, target);
      _propsNode.AddChild(propInstance);
      propInstance.SpawnFloatingText("Created", Colors.Green);
    }
  }
}
