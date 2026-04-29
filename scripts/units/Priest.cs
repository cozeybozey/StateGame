using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;

public partial class Priest : Unit
{
  private Node _unitsNode = null!;
  private Unit _unitToBeRevived = null!;

  protected override void Start()
  {
    _unitsNode = GetTree().CurrentScene.GetNode("Units");
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    // Pick random unit from dead units
    if (deadUnits.Count == 0)
      return [];
    List<Unit> friendlyDeadUnits = new List<Unit>();
    foreach (Unit unit in deadUnits)
    {
      if (unit.Side == Side)
        friendlyDeadUnits.Add(unit);
    }
    if (friendlyDeadUnits.Count == 0)
      return [];
    RandomNumberGenerator rng = new();
    _unitToBeRevived = friendlyDeadUnits[rng.RandiRange(0, friendlyDeadUnits.Count - 1)];
    UnitInfo unitInfo = _unitToBeRevived.GetStartInfo();

    // Spawn unit at random location
    List<Vector2I> possiblePositions = GlobalFunctions.GetPossibleGridEntityLocations(unitsGrid, terrainGrid, propsGrid, unitInfo.OccupiedCells, Side);

    // Now that the unit has been revided it can be removed from the dead units list
    if (IsInstanceValid(_unitToBeRevived))
      _unitToBeRevived.QueueFree();
    deadUnits.Remove(_unitToBeRevived);

    if (possiblePositions.Count > 0)
    {
      Vector2I cellPos = cellPos = possiblePositions[rng.RandiRange(0, possiblePositions.Count - 1)];
      return [cellPos];
    }
    else
      return [];
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    foreach (Vector2I target in targets)
    {
      // Spawn unit
      Unit unitInstance = GD.Load<PackedScene>(_unitToBeRevived.ScenePath).Instantiate() as Unit;
      unitInstance!.Initialize(_unitToBeRevived.GetStartInfo(), _unitToBeRevived.Side, target);
      unitInstance!.SetPlayData(unitsGrid, terrainGrid, propsGrid, units, deadUnits, _activeUnitsLayer, _targetedCellsLayer);
      _unitsNode.AddChild(unitInstance);
      unitInstance.SpawnFloatingText("Revived", Colors.Green);
    }
  }

  public static new int ScorePlacement(Vector2I pos, UnitInfo unitInfo, UnitInfo[,] unitsGrid, TerrainInfo[,] terrainGrid, PropInfo[,] propsGrid)
  {
    // Strongly prefer back rows
    return (Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5f) - pos.Y) + GlobalFunctions.StandardUnitScorePlacement(pos, unitInfo, unitsGrid, terrainGrid, propsGrid);
  }
}
