using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class WaterElemental : Unit
{
  private Node _propsNode = null!;
  private PropInfo _waterInfo = null!;

  protected override void Start()
  {
    _propsNode = GetTree().CurrentScene.GetNode("Props");
    _waterInfo = GlobalConstants.PropsData["pool_of_water"];
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> randomUnit = units
    .Where(u => u.Side != Side)
    .OrderBy(_ => GD.Randi())
    .Take(1)
    .Select(u => u.OccupiedMainCell)
    .ToList();

    return randomUnit;
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

      // Spawn pool of water
      if (!GlobalFunctions.CanSpawnProp(_waterInfo, target, unitsGrid, terrainGrid, propsGrid))
        continue;

      Prop propInstance = GD.Load<PackedScene>(_waterInfo.ScenePath).Instantiate() as Prop;
      propInstance!.Initialize(_waterInfo, target);
      _propsNode.AddChild(propInstance);
      propInstance.SpawnFloatingText("Created", Colors.Green);
    }
  }
}
