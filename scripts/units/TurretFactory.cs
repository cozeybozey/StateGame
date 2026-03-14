using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TurretFactory : Unit
{
  private UnitInfo turretUnitInfo = GlobalConstants.UnitsData["turret"];
  private Node _unitsNode = null!;

  protected override void Start()
  {
    _unitsNode = GetTree().CurrentScene.GetNode("Units");
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Vector2I? randomPos = GlobalFunctions.GetRandomUnitSpawnLocation(unitsGrid, turretUnitInfo.OccupiedCells, side);
    if (randomPos.HasValue)
      return [randomPos.Value];
    else
      return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid)
  {
    // Spawn unit
    foreach (Vector2I target in targets)
    {
      Unit unitInstance = GD.Load<PackedScene>(turretUnitInfo.ScenePath).Instantiate() as Unit;
      unitInstance!.Initialize(turretUnitInfo, side, target);
      _unitsNode.AddChild(unitInstance);
      unitInstance.SpawnFloatingText("Created", Colors.Green);
    }
  } 
}
