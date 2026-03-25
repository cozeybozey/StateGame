using Godot;
using System;
using System.Collections.Generic;

public partial class WildFire : Prop
{
  private Node _propsNode = null!;

  protected override void Start()
  {
    _propsNode = GetTree().CurrentScene.GetNode("Props");
  }

  public override void TurnEnd(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Unit unit = unitsGrid[OccupiedMainCell.X, OccupiedMainCell.Y];
    if (unit != null)
    {
      unit.ChangeHealth(-Damage, this);
    }

    List<Vector2I> surroundingCells = [new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(1, 0), new Vector2I(0, -1)];
    foreach (Vector2I cell in surroundingCells)
    {
      Vector2I targetLocation = OccupiedMainCell + cell;

      if (!GlobalFunctions.CanSpawnProp(this, targetLocation, terrainGrid, propsGrid))
        continue;

      Prop propInstance = GD.Load<PackedScene>(ScenePath).Instantiate() as Prop;
      propInstance!.Initialize(GetInfo(), targetLocation);
      _propsNode.AddChild(propInstance);
      propInstance.SpawnFloatingText("Created", Colors.Green);
    }
  }
}
