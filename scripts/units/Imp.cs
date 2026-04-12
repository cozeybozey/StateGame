using Godot;
using System;
using System.Collections.Generic;

public partial class Imp : Unit
{
  private Node _propsNode = null!;
  private PropInfo _fireInfo = null!;

  protected override void Start()
  {
    _propsNode = GetTree().CurrentScene.GetNode("Props");
    _fireInfo = GlobalConstants.PropsData["fire"];
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
