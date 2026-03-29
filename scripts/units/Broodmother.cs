using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Broodmother : Unit
{
  private UnitInfo parasiteUnitInfo = GlobalConstants.UnitsData["parasite"];
  private Node _unitsNode = null!;

  protected override void Start()
  {
    _unitsNode = GetTree().CurrentScene.GetNode("Units");
  }

  public override List<Vector2I> GetTargets(Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid, List<Unit> units, List<Unit> deadUnits)
  {
    List<Vector2I> positions = new List<Vector2I>();
    
    // Side is determined based on broodmother's position on the grid instead of its own side,
    // because broodmother could have been moved to the other side
    List<Vector2I> possibleSpawnLocations = GlobalFunctions.GetPossibleUnitLocations(unitsGrid, terrainGrid, propsGrid, parasiteUnitInfo.OccupiedCells, 
      OccupiedMainCell.Y > Mathf.FloorToInt(GlobalConstants.GridSize.Y * 0.5));

    foreach (Vector2I cell in GetSurroundingCells())
    {
      if (possibleSpawnLocations.Contains(cell))
        positions.Add(cell);

      if (positions.Count >= 4)
        break;
    }

    return positions;
  }

  public override void Act(List<Vector2I> targets, Unit[,] unitsGrid, Terrain[,] terrainGrid, Prop[,] propsGrid)
  {
    // Spawn unit
    foreach (Vector2I target in targets)
    {
      Unit unitInstance = GD.Load<PackedScene>(parasiteUnitInfo.ScenePath).Instantiate() as Unit;
      unitInstance!.Initialize(parasiteUnitInfo, Side, target);
      _unitsNode.AddChild(unitInstance);
      unitInstance.SpawnFloatingText("Created", Colors.Green);
    }
  }
}
