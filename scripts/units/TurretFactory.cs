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

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    Vector2I? randomPos = GlobalFunctions.GetRandomGridEntitySpawnLocation(unitsGrid, terrainGrid, propsGrid, turretUnitInfo.OccupiedCells, Side);
    if (randomPos.HasValue)
      return [randomPos.Value];
    else
      return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Spawn unit
    foreach (Vector2I target in targets)
    {
      Unit unitInstance = GD.Load<PackedScene>(turretUnitInfo.ScenePath).Instantiate() as Unit;
      unitInstance!.Initialize(turretUnitInfo, Side, target);
      unitInstance!.SetPlayData(unitsGrid, terrainGrid, propsGrid, units, deadUnits, _activeUnitsLayer, _targetedCellsLayer);
      _unitsNode.AddChild(unitInstance);
      unitInstance.SpawnFloatingText("Created", Colors.Green);
    }
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer back rows
    return (Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f) - pos.Y) + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
